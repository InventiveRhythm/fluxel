using System;
using System.Collections.Generic;
using System.Linq;
using fluxel.API;
using fluxel.Components;
using fluxel.Components.IPC;
using fluxel.Database.Extensions;
using fluxel.Models;
using fluxel.Models.Chat;
using fluxel.Models.Users;
using fluXis.Online.API.Models.Chat;
using fluXis.Online.API.Payloads.Chat;
using Microsoft.EntityFrameworkCore;
using Midori.API.Attributes;
using Midori.API.Components;
using Midori.Networking;

namespace fluxel.Services.API.Controllers;

[Controller("/chat")]
public class ChatController
{
    private readonly DatabaseContext database;
    private readonly ModelTranslator translator;
    private readonly Valkey valkey;

    public ChatController(DatabaseContext database, ModelTranslator translator, Valkey valkey)
    {
        this.database = database;
        this.translator = translator;
        this.valkey = valkey;
    }

    #region Channels

    [HttpRoute("/channels")]
    public APIReturn<APIChatChannel[]> ListChannels()
    {
        var channels = database.ChatChannels.Where(x => x.Type == APIChannelType.Public).ToArray();
        return channels.Select(translator.ToAPI).ToArray();
    }

    [Authenticated]
    [HttpRoute("/channels/joined")]
    public APIReturn<APIChatChannel[]> ListJoined(User auth)
    {
        var memberships = database.ChatChannelMembers
                                  .Include(x => x.Channel)
                                  .Where(x => x.UserID == auth.ID)
                                  .ToArray();

        return memberships.Select(x => translator.ToAPI(x.Channel!)).ToArray();
    }

    [Authenticated]
    [HttpRoute("/channels", APIMethod.Post)]
    public APIReturn<string> CreateChannel(User auth, [Source(ParameterSource.Body)] ChatCreateChannelPayload payload)
    {
        if (!database.Users.TryFind(out var target, payload.TargetID!.Value))
            return Returns.NotFound("user");

        if (!database.UserFollows.AreMutual(auth.ID, target.ID))
            return Returns.Message(HttpStatusCode.BadRequest, "You must follow each other to send direct messages.");

        var arr = new List<long> { auth.ID, target.ID };
        arr.Sort();

        var name = $"private_{string.Join("-", arr)}";

        using (database.EditAndSave())
        {
            var channel = database.ChatChannels.Find(name);
            channel ??= database.ChatChannels.Add(new ChatChannel(name, APIChannelType.Private)
            {
                TargetUserID1 = arr[0],
                TargetUserID2 = arr[1]
            }).Entity;

            if (!database.ChatChannelMembers.IsMemberOf(channel.ID, auth.ID))
                database.ChatChannelMembers.Add(new ChatChannelMember(channel.ID, auth.ID));

            // TODO: listen to this
            _ = valkey.Publish(Valkey.ChatChannelMemberAdd, new ValkeyChannelMemberEvent(channel.ID, auth.ID));
        }

        return name;
    }

    #endregion

    #region Messages

    [Authenticated]
    [HttpRoute("/channels/:channel/messages")]
    public APIReturn<APIChatMessage[]> GetMessages(User auth, string channel)
    {
        if (!database.ChatChannels.TryFind(out var ch, channel))
            return Returns.NotFound("channel");

        if (!database.ChatChannelMembers.Any(x => x.ChannelID == ch.ID && x.UserID == auth.ID))
            return Returns.Message(HttpStatusCode.Forbidden, "Current user is not part of this channel.");

        var messages = database.ChatMessages
                               .Where(x => x.ChannelID == ch.ID)
                               .OrderByDescending(x => x.CreatedAt)
                               .Take(50).ToArray();

        return messages.Select(translator.ToAPI).ToArray();
    }

    [Authenticated]
    [HttpRoute("/channels/:channel/messages", APIMethod.Post)]
    public APIReturn<APIChatMessage> SendMessage(User auth, string channel, [Source(ParameterSource.Body)] ChatMessagePayload payload)
    {
        if (!database.ChatChannels.TryFind(out var ch, channel))
            return Returns.NotFound("channel");

        if (!database.ChatChannelMembers.IsMemberOf(ch.ID, auth.ID))
            return Returns.Message(HttpStatusCode.Forbidden, "Current user is not part of this channel.");

        if (string.IsNullOrEmpty(payload.Content))
            return Returns.Message(HttpStatusCode.BadRequest, "Message cannot be empty.");

        if (payload.Content.Length > 2048)
            return Returns.Message(HttpStatusCode.BadRequest, "Message exceeds 2048 characters.");

        ChatMessage message;

        using (database.EditAndSave())
        {
            message = database.ChatMessages.Add(new ChatMessage(ch.ID, auth.ID, payload.Content)).Entity;

            if (ch.Type == APIChannelType.Private)
            {
                if (ch.TargetUserID1 != null && !database.ChatChannelMembers.IsMemberOf(ch.ID, ch.TargetUserID1.Value))
                {
                    database.ChatChannelMembers.Add(new ChatChannelMember(ch.ID, ch.TargetUserID1.Value));
                    _ = valkey.Publish(Valkey.ChatChannelMemberAdd, new ValkeyChannelMemberEvent(ch.ID, ch.TargetUserID1.Value));
                }

                if (ch.TargetUserID2 != null && !database.ChatChannelMembers.IsMemberOf(ch.ID, ch.TargetUserID2.Value))
                {
                    database.ChatChannelMembers.Add(new ChatChannelMember(ch.ID, ch.TargetUserID2.Value));
                    _ = valkey.Publish(Valkey.ChatChannelMemberAdd, new ValkeyChannelMemberEvent(ch.ID, ch.TargetUserID2.Value));
                }
            }
        }

        // TODO: listen
        _ = valkey.Publish(Valkey.ChatMessageCreate, new ValkeyStringIDEvent(message.ID));
        return translator.ToAPI(message);
    }

    [Authenticated(Scopes.MOD)]
    [HttpRoute("/channels/:channel/messages/:message", APIMethod.Delete)]
    public APIReturn<object> DeleteMessage(User auth, string channel, string message)
    {
        if (!database.ChatChannels.TryFind(out var ch, channel))
            return Returns.NotFound("channel");

        if (!database.ChatMessages.TryFind(out var msg, message))
            return Returns.NotFound("message");

        using (database.EditAndSave())
            msg.DeletedAt = DateTime.UtcNow;

        _ = valkey.Publish(Valkey.ChatMessageDelete, new ValkeyStringIDEvent(msg.ID));
        return Returns.Okay();
    }

    #endregion

    #region Users

    [Authenticated]
    [HttpRoute("/channels/:channel/users/:userid", APIMethod.Put)]
    public APIReturn<object> JoinChannel(User auth, string channel, long userid)
    {
        if (!database.ChatChannels.TryFind(out var ch, channel))
            return Returns.NotFound("channel");

        if (ch.Type != APIChannelType.Public)
            return Returns.Message(HttpStatusCode.Forbidden, "You can only join public channels.");
        if (userid != auth.ID)
            return Returns.Message(HttpStatusCode.Forbidden, "You cannot add other people to channels.");

        bool member = database.ChatChannelMembers.IsMemberOf(channel, userid);

        if (!member)
        {
            database.ChatChannelMembers.Add(new ChatChannelMember(channel, userid));
            database.SaveChanges();
        }

        // we push this regardless in case the client is desynced
        _ = valkey.Publish(Valkey.ChatChannelMemberAdd, new ChatChannelMember(channel, userid));
        return !member ? Returns.Created() : Returns.NotModified();
    }

    [Authenticated]
    [HttpRoute("/channels/:channel/users/:userid", APIMethod.Delete)]
    public APIReturn<object> LeaveChannel(User auth, string channel, long userid)
    {
        if (!database.ChatChannels.TryFind(out var ch, channel))
            return Returns.NotFound("channel");

        if (ch.Type != APIChannelType.Public)
            return Returns.Message(HttpStatusCode.Forbidden, "You can only leave public channels.");
        if (userid != auth.ID)
            return Returns.Message(HttpStatusCode.Forbidden, "You cannot remove other people to channels.");

        var entry = database.ChatChannelMembers.Find(channel, userid);

        if (entry != null)
        {
            database.ChatChannelMembers.Remove(entry);
            database.SaveChanges();
        }

        _ = valkey.Publish(Valkey.ChatChannelMemberRemove, new ChatChannelMember(channel, userid));
        return entry != null ? Returns.Okay() : Returns.NotModified();
    }

    #endregion
}
