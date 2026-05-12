using System;
using System.Collections.Generic;
using System.Linq;
using fluxel.Models.Chat;
using fluXis.Online.API.Models.Chat;
using Midori.Database;

namespace fluxel.Database;

public class ChatManager
{
    private readonly IDatabaseTable<ChatMessage> messages;
    private readonly IDatabaseTable<ChatChannel> channels;

    public ChatManager(IDatabaseProvider db)
    {
        messages = db.GetTable<ChatMessage>("chat_messages");
        channels = db.GetTable<ChatChannel>("chat-channels");
    }

    #region Messages

    public ChatMessage Add(long uid, string content, string channel, ulong? discord = null)
    {
        var message = new ChatMessage
        {
            SenderID = uid,
            Content = content,
            Channel = channel,
            DiscordID = discord
        };

        messages.Add(message);
        return message;
    }

    public ChatMessage? Get(string channel, string id) => Guid.TryParse(id, out var g) ? Get(channel, g) : null;
    public ChatMessage? Get(string channel, Guid id) => messages.Find(x => x.Channel == channel && x.ID == id).FirstOrDefault();
    public ChatMessage? Get(Guid id) => messages.Find(x => x.ID == id).FirstOrDefault();
    public ChatMessage? GetByDiscordID(ulong id) => messages.Find(x => x.DiscordID == id).FirstOrDefault();

    public void AttachDiscordID(Guid msg, ulong id)
    {
        var message = messages.Find(x => x.ID == msg).FirstOrDefault();
        if (message is null) return;

        message.DiscordID = id;
        messages.Replace(x => x.ID == msg, message);
    }

    public void Delete(ChatMessage message)
    {
        message.Deleted = true;
        messages.Replace(x => x.ID == message.ID, message);
    }

    public IEnumerable<ChatMessage> FromChannel(string channel) => messages.Find(x => x.Channel == channel && !x.Deleted).ToList();

    #endregion

    #region Channels

    public IReadOnlyList<ChatChannel> PublicChannels => channels.Find(x => x.Type == APIChannelType.Public).ToList();

    public void CreatePublicChannel(string name, List<long>? ids = default) => channels.Add(new ChatChannel(name, APIChannelType.Public)
    {
        Users = ids ?? new List<long>()
    });

    public ChatChannel CreateDirectChannel(string name, long one, long two)
    {
        var chan = new ChatChannel(name, APIChannelType.Private)
        {
            Target1 = one,
            Target2 = two
        };

        channels.Add(chan);
        return chan;
    }

    public ChatChannel CreateClubChannel(string name, long club)
    {
        var chan = new ChatChannel(name, APIChannelType.Club)
        {
            Club = club
        };

        channels.Add(chan);
        return chan;
    }

    public ChatChannel? GetChannel(string name) => channels.Find(x => x.Name.ToLowerInvariant() == name.ToLowerInvariant()).FirstOrDefault();

    public IEnumerable<ChatChannel> WithMember(long id) => channels.Find(x => x.Users.Contains(id)).ToList();

    public bool AddToChannel(string name, long id)
    {
        var chan = GetChannel(name);

        if (chan is null || chan.Users.Contains(id))
            return false;

        chan.Users.Add(id);
        Update(chan);
        return true;
    }

    public bool RemoveFromChannel(string name, long id)
    {
        var chan = GetChannel(name);

        if (chan is null || !chan.Users.Contains(id))
            return false;

        chan.Users.Remove(id);
        Update(chan);
        return true;
    }

    public void Update(ChatChannel channel)
        => channels.Replace(x => x.Name == channel.Name, channel);

    #endregion
}
