using fluxel.Database.Helpers;
using fluxel.Modules;
using fluxel.Modules.Messages;
using fluxel.Tasks.Management;
using fluXis.Online.API.Models.Users;
using Midori.Networking;
using osu.Framework.Extensions.IEnumerableExtensions;

namespace fluxel.Social;

public class NotificationsModule : IModule, IOnlineStateManager
{
    public static HttpConnectionManager<NotificationSocket> Sockets { get; private set; } = null!;

    public void OnLoad(ServerHost host)
    {
        fixInvalidOnlineStates();

        Sockets = host.Server.MapModule<NotificationSocket>("/notifications");
        host.Scheduler.Schedule(new CleanupOnlineStatesCronTask());
    }

    public void OnMessage(object data)
    {
        switch (data)
        {
            case UserCollectionMessage coll:
            {
                Sockets.Where(x => x.UserID == coll.UserID).ForEach(x => x.Client.CollectionUpdated(coll.CollectionID, coll.Added, coll.Changed, coll.Removed));
                break;
            }

            case UserAchievementMessage ach:
            {
                Sockets.Where(x => x.UserID == ach.UserID).ForEach(x => x.Client.RewardAchievement(ach.Achievement));
                break;
            }

            case UserNotificationMessage notif:
            {
                Sockets.Where(x => x.UserID == notif.UserID).ForEach(x => x.Client.NotificationReceived(notif.Notification));
                break;
            }
        }
    }

    private static void fixInvalidOnlineStates()
    {
        var online = UserHelper.LastOnlineLogs();
        online.ForEach(x => UserHelper.LogOnline(x, false));
    }

    long[] IOnlineStateManager.AllOnline => Sockets.Select(x => x.UserID).ToArray();

    bool IOnlineStateManager.IsOnline(long user) => Sockets.Any(x => x.UserID == user);

    APIActivity? IOnlineStateManager.GetActivity(long user)
    {
        var conn = Sockets.FirstOrDefault(x => x.UserID == user);
        if (conn?.Activity is null) return null;

        return new APIActivity
        {
            Name = conn.Activity.Value.name,
            Data = conn.Activity.Value.data
        };
    }
}
