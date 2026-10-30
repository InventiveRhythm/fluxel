using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace fluxel.Models.Notifications;

public class ClubInviteNotification : Notification
{
    [Column("invite_code"), StringLength(7)]
    public string ClubInviteCode { get; init; } = string.Empty;
}
