using System.ComponentModel.DataAnnotations.Schema;
using fluXis.Online.API.Models.Maps.Modding;

namespace fluxel.Models.Notifications;

public class QueueNotification : Notification
{
    [Column("mapset")]
    public long MapSet { get; init; }

    [Column("queue_action")]
    public APIModdingActionType QueueAction { get; init; }
}
