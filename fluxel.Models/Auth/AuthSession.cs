using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using fluxel.Models.Users;

namespace fluxel.Models.Auth;

[Table("auth_session")]
public class AuthSession
{
    #region Columns

    [Key, Column("id"), StringLength(32)]
    public string ID { get; init; } = Guid.NewGuid().ToString("N");

    [Column("user")]
    public long UserID { get; init; }

    [Column("token"), StringLength(32)]
    public string Token { get; init; } = string.Empty;

    [Column("ip"), MaxLength(20)]
    public string IPAddress { get; init; } = string.Empty;

    [Column("user_agent"), MaxLength(256)]
    public string UserAgent { get; init; } = string.Empty;

    [Column("country"), StringLength(2)]
    public string? Country { get; init; } = string.Empty;

    [Column("location"), MaxLength(128)]
    public string? Location { get; init; } = string.Empty;

    [Column("last_activity")]
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;

    #endregion

    #region Relations

    [ForeignKey(nameof(UserID))]
    public User? User { get; init; }

    #endregion
}
