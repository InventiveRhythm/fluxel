using fluxel.API.Components;
using fluxel.Models.Notifications;
using fluxel.Modules.Messages;

namespace fluxel.Tasks.Users;

public class SendNotificationTask : IBasicTask
{
    public string Name => $"SendNotification({notification.UserID}, {notification.ID})";

    private Notification notification { get; }

    public SendNotificationTask(Notification notification)
    {
        this.notification = notification;
    }

    public void Run()
    {
        var notif = notification.ToAPI(new RequestCache());
        if (notif is null) return;

        ServerHost.Instance.SendMessage(new UserNotificationMessage(notification.UserID, notif));
    }
}
