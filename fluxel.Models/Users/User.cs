using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using fluxel.Models.Chat;
using fluxel.Models.Clubs;
using fluxel.Models.Groups;
using fluxel.Models.Maps;
using fluxel.Models.Users.Flags;
using Microsoft.EntityFrameworkCore;

namespace fluxel.Models.Users;

[Index(nameof(Username), IsUnique = true)]
[Table("user")]
public class User
{
    #region Columns

    [Key, Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long ID { get; init; }

    [Column("discord_id")]
    public ulong? DiscordID { get; set; }

    [Column("steam_id")]
    public ulong? SteamID { get; set; }

    [Column("name"), MaxLength(16), Required]
    public string Username { get; set; } = "";

    [Column("last_name_change"), DefaultValue(0)]
    public DateTime? LastNameChange { get; set; }

    [Column("force_name_change"), DefaultValue(false)]
    public bool ForceNameChange { get; set; }

    [Column("last_notification_read"), DefaultValue(0)]
    public DateTime? LastNotificationRead { get; set; }

    [Column("support_end")]
    public DateTime? SupportEndTime { get; set; }

    [Column("club")]
    public long? ClubID { get; set; }

    [Column("ban_flags"), DefaultValue(0)]
    public UserBanFlag BanFlags { get; set; }

    [Column("country"), StringLength(2)]
    public string? CountryCode { get; set; } = string.Empty;

    [Column("created"), DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    [Column("last_login"), DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public DateTime LastLogin { get; set; } = DateTime.UtcNow;

    #endregion

    #region Relations

    [ForeignKey(nameof(ClubID))]
    public Club? Club { get; set; }

    [InverseProperty(nameof(UserProfile.User))]
    public UserProfile? Profile { get; init; }

    [InverseProperty(nameof(UserStatistics.User))]
    public ICollection<UserStatistics> Statistics { get; init; } = [];

    [InverseProperty(nameof(ChatChannelMember.User))]
    public ICollection<ChatChannelMember> ChannelMemberships { get; set; } = [];

    [InverseProperty(nameof(GroupMember.User))]
    public ICollection<GroupMember> GroupMemberships { get; set; } = [];

    [InverseProperty(nameof(MapUserState.User))]
    public ICollection<MapUserState> MapStates { get; init; } = [];

    #endregion

    [NotMapped]
    public bool IsSupporter => SupportEndTime > DateTime.UtcNow /*|| this.IsPurifier() || this.IsModerator()*/;
}
