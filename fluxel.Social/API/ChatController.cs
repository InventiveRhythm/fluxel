using fluxel.API;
using fluxel.Components;
using fluxel.Database;
using fluxel.Models.Users;
using fluxel.Modules;
using fluxel.Modules.Messages.Chat;
using fluXis.Online.API.Models.Chat;
using fluXis.Online.API.Payloads.Chat;
using Midori.API.Attributes;
using Midori.API.Components;
using Midori.Networking;

namespace fluxel.Social.API;

[Controller("/chat")]
public class ChatController
{
    private readonly ChatManager chats;
    private readonly UserManager users;
    private readonly ModelTranslator translator;
    private readonly NotificationsModule module;
    private readonly ModuleManager modules;

    public ChatController(ChatManager chats, ModelTranslator translator, NotificationsModule module, UserManager users, ModuleManager modules)
    {
        this.chats = chats;
        this.translator = translator;
        this.module = module;
        this.users = users;
        this.modules = modules;
    }

    #region Channels

    [HttpRoute("/channels")]
    public APIReturn<List<APIChatChannel>> ListChannels()
        => chats.PublicChannels.Select(translator.ToAPI).ToList();

    [Authenticated]
    [HttpRoute("/channels/joined")]
    public APIReturn<List<APIChatChannel>> ListJoined(User auth)
        => chats.WithMember(auth.ID).Select(translator.ToAPI).ToList();

    [Authenticated]
    [HttpRoute("/channels", APIMethod.Post)]
    public APIReturn<string> CreateChannel(User auth, [Source(ParameterSource.Body)] ChatCreateChannelPayload payload)
    {
        if (!users.TryGet(payload.TargetID!.Value, out var target))
            return Returns.NotFound("user");
        if (!users.Mutual(auth.ID, target.ID))
            return Returns.Message(HttpStatusCode.BadRequest, "You must follow each other to send direct messages.");

        var arr = new List<long> { auth.ID, target.ID };
        arr.Sort();

        var name = $"private_{string.Join("-", arr)}";
        var channel = chats.GetChannel(name) ?? chats.CreateDirectChannel(name, arr[0], arr[1]);

        chats.AddToChannel(channel.Name, auth.ID);
        module.SocketByID(auth.ID)?.Client.AddToChatChannel(translator.ToAPI(channel));
        return channel.Name;
    }

    #endregion

    #region Messages

    [Authenticated]
    [HttpRoute("/channels/:channel/messages")]
    public APIReturn<List<APIChatMessage>> GetMessages(User auth, string channel)
    {
        var ch = chats.GetChannel(channel);

        if (ch is null)
            return Returns.Message(HttpStatusCode.NotFound, "This channel does not exist.");

        if (!ch.Users.Contains(auth.ID))
            return Returns.Message(HttpStatusCode.Forbidden, "Current user is not part of this channel.");

        var messages = chats.FromChannel(channel)
                            .OrderByDescending(x => x.CreatedAt)
                            .ToList().Take(50);

        return messages.Select(translator.ToAPI).ToList();
    }

    [Authenticated]
    [HttpRoute("/channels/:channel/messages", APIMethod.Post)]
    public APIReturn<APIChatMessage> SendMessage(User auth, string channel, [Source(ParameterSource.Body)] ChatMessagePayload payload)
    {
        var ch = chats.GetChannel(channel);

        if (ch is null)
            return Returns.Message(HttpStatusCode.NotFound, "The specified channel does not exist.");

        if (!ch.Users.Contains(auth.ID))
            return Returns.Message(HttpStatusCode.Forbidden, "Current user is not part of this channel.");

        if (string.IsNullOrEmpty(payload.Content))
            return Returns.Message(HttpStatusCode.BadRequest, "Message cannot be empty.");

        if (payload.Content.Length > 2048)
            return Returns.Message(HttpStatusCode.BadRequest, "Message exceeds 2048 characters.");

        var message = chats.Add(auth.ID, payload.Content, ch.Name);

        if (ch.Type == APIChannelType.Private)
        {
            var api = translator.ToAPI(ch);

            if (chats.AddToChannel(ch.Name, ch.Target1!.Value))
                module.SocketByID(ch.Target1.Value)?.Client.AddToChatChannel(api);
            if (chats.AddToChannel(ch.Name, ch.Target2!.Value))
                module.SocketByID(ch.Target2.Value)?.Client.AddToChatChannel(api);
        }

        modules.SendMessage(new ChatMessageCreateMessage(message.ID));
        return translator.ToAPI(message);
    }

    [Authenticated(Scopes.MOD)]
    [HttpRoute("/channels/:channel/messages/:message", APIMethod.Delete)]
    public APIReturn<object> DeleteMessage(User auth, string channel, string message)
    {
        var msg = chats.Get(channel, message);

        if (msg is null)
            return Returns.Message(HttpStatusCode.NotFound, "Message not found.");

        chats.Delete(msg);
        modules.SendMessage(new ChatMessageDeleteMessage(msg.ID));
        return Returns.Okay();
    }

    #endregion

    #region Users

    [Authenticated]
    [HttpRoute("/channels/:channel/users/:userid", APIMethod.Put)]
    public APIReturn<object> JoinChannel(User auth, string channel, long userid)
    {
        var chan = chats.GetChannel(channel);

        if (chan == null)
            return Returns.Message(HttpStatusCode.NotFound, "channel not found");
        if (chan.Type != APIChannelType.Public)
            return Returns.Message(HttpStatusCode.Forbidden, "you can only join public channels");
        if (userid != auth.ID)
            return Returns.Message(HttpStatusCode.Forbidden, "you cannot add other people to channels");

        var result = chats.AddToChannel(chan.Name, userid);
        module.SocketByID(userid)?.Client.AddToChatChannel(translator.ToAPI(chan));
        return result ? Returns.Created() : Returns.NotModified();
    }

    [Authenticated]
    [HttpRoute("/channels/:channel/users/:userid", APIMethod.Delete)]
    public APIReturn<object> LeaveChannel(User auth, string channel, long userid)
    {
        var chan = chats.GetChannel(channel);

        if (chan == null)
            return Returns.Message(HttpStatusCode.NotFound, "channel not found");
        if (chan.Type != APIChannelType.Public)
            return Returns.Message(HttpStatusCode.Forbidden, "you can only leave public channels");
        if (userid != auth.ID)
            return Returns.Message(HttpStatusCode.Forbidden, "you cannot remove other people from channels");

        var result = chats.RemoveFromChannel(chan.Name, userid);
        module.SocketByID(userid)?.Client.RemoveFromChatChannel(channel);
        return result ? Returns.Okay() : Returns.NotModified();
    }

    #endregion
}
