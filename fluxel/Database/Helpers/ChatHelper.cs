using System;
using System.Collections.Generic;
using fluxel.Models.Chat;
using fluXis.Online.API.Models.Chat;
using MongoDB.Driver;
using ChatMessage = fluxel.Models.Chat.ChatMessage;

namespace fluxel.Database.Helpers;

public static class ChatHelper
{
    #region Messages

    private static IMongoCollection<ChatMessage> messages => MongoDatabase.GetCollection<ChatMessage>("chat_messages");

    public static ChatMessage Add(long uid, string content, string channel, ulong? discord = null)
    {
        var message = new ChatMessage
        {
            SenderID = uid,
            Content = content,
            Channel = channel,
            DiscordID = discord
        };

        messages.InsertOne(message);
        return message;
    }

    public static ChatMessage? Get(string channel, string id) => Guid.TryParse(id, out var g) ? Get(channel, g) : null;
    public static ChatMessage? Get(string channel, Guid id) => messages.Find(x => x.Channel == channel && x.ID == id).FirstOrDefault();
    public static ChatMessage? Get(Guid id) => messages.Find(x => x.ID == id).FirstOrDefault();
    public static ChatMessage? GetByDiscordID(ulong id) => messages.Find(x => x.DiscordID == id).FirstOrDefault();

    public static void AttachDiscordID(Guid msg, ulong id)
    {
        var message = messages.Find(x => x.ID == msg).FirstOrDefault();
        if (message is null) return;

        message.DiscordID = id;
        messages.ReplaceOne(x => x.ID == msg, message);
    }

    public static void Delete(ChatMessage message)
    {
        message.Deleted = true;
        messages.ReplaceOne(x => x.ID == message.ID, message);
    }

    public static IEnumerable<ChatMessage> FromChannel(string channel) => messages.Find(x => x.Channel == channel && !x.Deleted).ToList();

    #endregion

    #region Channels

    private static IMongoCollection<ChatChannel> channels => MongoDatabase.GetCollection<ChatChannel>("chat-channels");

    public static IReadOnlyList<ChatChannel> PublicChannels => channels.Find(x => x.Type == APIChannelType.Public).ToList();

    public static void CreatePublicChannel(string name, List<long>? ids = default) => channels.InsertOne(new ChatChannel(name, APIChannelType.Public)
    {
        Users = ids ?? new List<long>()
    });

    public static ChatChannel CreateDirectChannel(string name, long one, long two)
    {
        var chan = new ChatChannel(name, APIChannelType.Private)
        {
            Target1 = one,
            Target2 = two
        };

        channels.InsertOne(chan);
        return chan;
    }

    public static ChatChannel CreateClubChannel(string name, long club)
    {
        var chan = new ChatChannel(name, APIChannelType.Club)
        {
            Club = club
        };

        channels.InsertOne(chan);
        return chan;
    }

    public static ChatChannel? GetChannel(string name) => channels.Find(x => x.Name.ToLowerInvariant() == name.ToLowerInvariant()).FirstOrDefault();

    public static IEnumerable<ChatChannel> WithMember(long id) => channels.Find(x => x.Users.Contains(id)).ToList();

    public static bool AddToChannel(string name, long id)
    {
        var chan = GetChannel(name);

        if (chan is null || chan.Users.Contains(id))
            return false;

        chan.Users.Add(id);
        Update(chan);
        return true;
    }

    public static bool RemoveFromChannel(string name, long id)
    {
        var chan = GetChannel(name);

        if (chan is null || !chan.Users.Contains(id))
            return false;

        chan.Users.Remove(id);
        Update(chan);
        return true;
    }

    public static void Update(ChatChannel channel)
        => channels.ReplaceOne(x => x.Name == channel.Name, channel);

    #endregion
}
