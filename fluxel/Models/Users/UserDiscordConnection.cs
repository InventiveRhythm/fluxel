using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace fluxel.Models.Users;

[Table("users-discord")]
public class UserDiscordConnection
{
    [Key, Column("_id"), Required]
    public long ID { get; init; }

    [Column("token"), Required, MaxLength(64)]
    public string AccessToken { get; init; } = string.Empty;

    [Column("refresh"), Required, MaxLength(32)]
    public string RefreshToken { get; init; } = string.Empty;

    [Column("expire"), Required]
    public DateTimeOffset Expire { get; init; }

    [ForeignKey(nameof(ID))]
    public User User { get; init; } = null!;
}
