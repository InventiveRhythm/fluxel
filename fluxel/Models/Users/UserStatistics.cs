using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace fluxel.Models.Users;

[Table("users-stats")]
[Index(nameof(UserID), nameof(Mode), Name = "user_mode", IsUnique = true)]
public class UserStatistics
{
    [Key, Column("_id"), MaxLength(36)]
    public string ID { get; init; } = Guid.NewGuid().ToString();

    [Column("user"), Required]
    public long UserID { get; init; }

    [Column("mode"), MaxLength(64), Required]
    public string Mode { get; init; } = string.Empty;

    [Column("ovr")]
    public double OverallRating { get; set; }

    [Column("ptr")]
    public double PotentialRating { get; set; }

    [ForeignKey(nameof(UserID))]
    public User User { get; init; } = null!;
}
