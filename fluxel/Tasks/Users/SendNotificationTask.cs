using System;
using System.Threading.Tasks;
using fluxel.Components;
using fluxel.Models.Notifications;
using fluxel.Modules;
using fluxel.Modules.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace fluxel.Tasks.Users;

public class SendNotificationTask : IBasicTask
{
    public string Name => $"SendNotification({notification.UserID}, {notification.ID})";

    private Notification notification { get; }

    public SendNotificationTask(Notification notification)
    {
        this.notification = notification;
    }

    public Task Run(IServiceProvider services)
    {
        var notif = services.GetRequiredService<ModelTranslator>().ToAPI(notification);
        if (notif is null) return Task.CompletedTask;

        services.GetRequiredService<ModuleManager>().SendMessage(new UserNotificationMessage(notification.UserID, notif));
        return Task.CompletedTask;
    }
}
