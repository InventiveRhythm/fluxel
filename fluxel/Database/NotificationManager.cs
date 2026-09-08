using System.Collections.Generic;
using System.Linq;
using fluxel.Models.Notifications;
using fluxel.Tasks;
using fluxel.Tasks.Users;

namespace fluxel.Database;

public class NotificationManager : DatabaseManager
{
    private readonly TaskRunner tasks;

    public NotificationManager(DatabaseContext database, TaskRunner tasks)
        : base(database)
    {
        this.tasks = tasks;
    }

    public Notification Create(Notification notification)
    {
        using (Database.EditAndSave())
            Database.Notifications.Add(notification);

        tasks.Schedule(new SendNotificationTask(notification));
        return notification;
    }

    public List<Notification> ForUser(long id)
        => [.. Database.Notifications.Where(x => x.UserID == id)];
}
