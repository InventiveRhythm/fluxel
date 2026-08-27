using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using fluxel.Components;
using fluxel.Database;
using fluxel.Models.Other;
using fluxel.Models.Users;
using fluXis.Online.API.Models.Clubs;
using Microsoft.EntityFrameworkCore;

namespace fluxel.Models.Clubs;

[Index(nameof(Tag), IsUnique = true)]
[Table(ClubManager.TABLE_NAME)]
public class Club : IHasID
{
    [Key]
    [Column("_id")]
    public long ID { get; set; }

    [Column("name")]
    [MaxLength(24)]
    public string Name { get; set; } = "";

    [Column("tag")]
    [MinLength(3), MaxLength(5)]
    public string Tag { get; set; } = "";

    [Column("icon")]
    [MaxLength(64)]
    public string IconHash { get; set; } = "";

    [Column("banner")]
    [MaxLength(64)]
    public string BannerHash { get; set; } = "";

    [Column("join-type")]
    public ClubJoinType JoinType { get; set; }

    [Column("color")]
    public List<GradientColor> Colors { get; set; } = new();

    [Column("owner")]
    public long OwnerID { get; set; }

    [Column("members")]
    public List<long> Members { get; set; } = new();

    [Column("ovr")]
    public double OverallRating { get; set; }

    [Column("score")]
    public long TotalScore { get; set; }

    [NotMapped]
    public string ChatChannel => $"club_{ID}";

    public bool IsInClub(User user) => IsInClub(user.ID);
    public bool IsInClub(long user) => Members.Contains(user);

    public void AddMember(long user, ClubManager clubs, ChatManager chats)
    {
        using (clubs.EditAndSave())
            Members.Add(user);

        chats.AddToChannel(ChatChannel, user);
    }

    public void RemoveMember(long user, ClubManager clubs, ChatManager chats)
    {
        using (clubs.EditAndSave())
            Members.Remove(user);

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
