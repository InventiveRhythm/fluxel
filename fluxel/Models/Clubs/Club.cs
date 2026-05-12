using System;
using System.Collections.Generic;
using System.Linq;
using fluxel.Components;
using fluxel.Database;
using fluxel.Models.Other;
using fluxel.Models.Users;
using fluXis.Online.API.Models.Clubs;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace fluxel.Models.Clubs;

[JsonObject(MemberSerialization.OptIn)]
public class Club : IHasID
{
    [BsonId]
    public long ID { get; set; }

    [BsonElement("name")]
    public string Name { get; set; } = "";

    [BsonElement("tag")]
    public string Tag { get; set; } = "";

    [BsonElement("icon")]
    public string IconHash { get; set; } = "";

    [BsonElement("banner")]
    public string BannerHash { get; set; } = "";

    [BsonElement("join-type")]
    public ClubJoinType JoinType { get; set; }

    [BsonElement("color")]
    public List<GradientColor> Colors { get; set; } = new();

    [BsonElement("owner")]
    public long OwnerID { get; set; }

    [BsonElement("members")]
    public List<long> Members { get; set; } = new();

    [BsonElement("ovr")]
    public double OverallRating { get; set; }

    [BsonElement("score")]
    public long TotalScore { get; set; }

    [BsonIgnore]
    public string ChatChannel => $"club_{ID}";

    public bool IsInClub(User user) => IsInClub(user.ID);
    public bool IsInClub(long user) => Members.Contains(user);

    public void AddMember(long user, ClubManager clubs, ChatManager chats)
    {
        Members.Add(user);
        clubs.Update(this);
        chats.AddToChannel(ChatChannel, user);
    }

    public void RemoveMember(long user, ClubManager clubs, ChatManager chats)
    {
        Members.Remove(user);
        clubs.Update(this);
        chats.RemoveFromChannel(ChatChannel, user);
    }

    public User? GetOwner(UserManager users, RequestCache? cache = null)
        => cache?.Users.Get(OwnerID) ?? users.Get(OwnerID);

    public List<User> GetMemberList(UserManager users, RequestCache? cache = null)
        => Members.Select(x => cache?.Users.Get(x) ?? users.Get(x)).OfType<User>().ToList();
}

[Flags]
public enum ClubIncludes
{
    Owner = 1 << 0,
    JoinType = 1 << 1,
    Members = 1 << 2,
    Statistics = 1 << 3,

    Everything = int.MaxValue
}
