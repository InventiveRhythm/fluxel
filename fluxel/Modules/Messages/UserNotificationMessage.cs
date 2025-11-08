using fluXis.Online.API.Models.Notifications;

namespace fluxel.Modules.Messages;

public class UserNotificationMessage
{
    public long UserID { get; }
    public APINotification Notification { get; }

    public UserNotificationMessage(long userID, APINotification notification)
    {
        UserID = userID;
        Notification = notification;
    }
}
