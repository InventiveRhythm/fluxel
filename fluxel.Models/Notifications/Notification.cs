using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace fluxel.Models.Notifications;

[Table("notification")]
public class Notification
{
    [Key, Column("id"), StringLength(32)]
    public string ID { get; init; } = Guid.NewGuid().ToString("N");

    [Column("user"), Required]
    public long UserID { get; init; }

    [Column("time"), Required]
    public DateTime Time { get; init; } = DateTime.UtcNow;
}
