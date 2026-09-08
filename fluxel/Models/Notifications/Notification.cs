using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using fluXis.Online.API.Models.Maps.Modding;
using fluXis.Online.API.Models.Notifications;
using JetBrains.Annotations;

namespace fluxel.Models.Notifications;

[Table("notifications")]
public class Notification
{
    [Key, Column("_id"), Required, MaxLength(36)]
    public string ID { get; init; } = Guid.NewGuid().ToString();

    [Column("user"), Required]
    public long UserID { get; set; }

    [Column("type"), Required]
    public NotificationType Type { get; set; }

    [Column("time"), Required]
    public DateTime Time { get; set; } = DateTime.UtcNow;

    #region Extra Data

    [Column("club-invite-code"), MaxLength(7)]
    public string? ClubInviteCode { get; init; }

    [Column("mapset")]
    public long? MapSet { get; init; }

    [Column("queue-action")]
    public APIModdingActionType? QueueAction { get; init; }

    #endregion

    public Notification(long id, NotificationType type)
    {
        UserID = id;
        Type = type;
    }

    [UsedImplicitly]
    private Notification()
    {
    }
}
