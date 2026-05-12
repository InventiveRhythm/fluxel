using System.Collections.Generic;
using System.Linq;
using fluxel.Models.Notifications;
using fluxel.Tasks;
using fluxel.Tasks.Users;
using Midori.Database;

namespace fluxel.Database;

public class NotificationManager
{
    private readonly IDatabaseTable<Notification> notifications;

    private readonly TaskRunner tasks;

    public NotificationManager(IDatabaseProvider db, TaskRunner tasks)
    {
        notifications = db.GetTable<Notification>("notifications");
        this.tasks = tasks;
    }

    public Notification Create(Notification notification)
    {
        notifications.Add(notification);

        tasks.Schedule(new SendNotificationTask(notification));
        return notification;
    }

    public List<Notification> ForUser(long id)
        => notifications.Find(x => x.UserID == id).ToList();
}
