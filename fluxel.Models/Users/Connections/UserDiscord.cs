using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace fluxel.Models.Users.Connections;

[Table("user_discord")]
public class UserDiscord
{
    #region Columns

    [Key, Column("id"), Required]
    public long UserID { get; init; }

    [Column("token"), Required, MaxLength(64)]
    public string AccessToken { get; set; } = string.Empty;

    [Column("refresh"), Required, MaxLength(32)]
    public string RefreshToken { get; set; } = string.Empty;

    [Column("expire"), Required]
    public DateTime Expire { get; set; }

    #endregion

    #region Relations

    [ForeignKey(nameof(UserID))]
    public User User { get; init; } = null!;

    #endregion
}
