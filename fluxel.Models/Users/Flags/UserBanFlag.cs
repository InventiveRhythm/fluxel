using System;
using System.ComponentModel;

namespace fluxel.Models.Users.Flags;

[Flags]
public enum UserBanFlag : ulong
{
    /// <summary>
    /// Can't upload maps.
    /// </summary>
    [Description("Banned from uploading maps.")]
    UploadBan = 1L << 0,

    /// <summary>
    /// Can't chat.
    /// </summary>
    [Description("Banned from chat.")]
    ChatBan = 1L << 1,

    /// <summary>
    /// Can not join open lobbies.
    /// </summary>
    [Description("Banned from multiplayer.")]
    MultiplayerBan = 1L << 2,

    /// <summary>
    /// Can not play ranked.
    /// </summary>
    [Description("Banned from ranked.")]
    RankedBan = 1L << 3,

    /// <summary>
    /// Can not post scores.
    /// </summary>
    [Description("Banned from leaderboards.")]
    LeaderboardBan = 1L << 4,

    /// <summary>
    /// Can't change their avatar and banner anymore.
    /// </summary>
    [Description("Banned from changing their avatar and banner.")]
    AvatarBannerBan = 1L << 5,

    /// <summary>
    /// Can't change anything on their profile anymore
    /// </summary>
    [Description("Banned from changing anything on their profile.")]
    ProfileBan = 1L << 6
}
