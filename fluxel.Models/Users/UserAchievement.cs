using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace fluxel.Models.Users;

[PrimaryKey(nameof(UserID), nameof(AchievementID))]
[Table("user_achievement")]
public class UserAchievement
{
    #region Columns

    [Column("user")]
    public long UserID { get; init; }

    [Column("achievement"), MaxLength(64)]
    public string AchievementID { get; init; } = string.Empty;

    [Column("time")]
    public DateTime Timestamp { get; init; }

    #endregion

    #region Relations

    [ForeignKey(nameof(UserID))]
    public User? User { get; init; }

    #endregion
}
