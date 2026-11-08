using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fluxel.Components;
using fluxel.Components.IPC;
using fluxel.WebSocket;
using fluXis.Online.API.Models.Users;
using fluXis.Online.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Midori.Utils;
using Newtonsoft.Json.Linq;

namespace fluxel.Services.Beacon;

public class NotificationSocket : AuthenticatedSocket<INotificationServer, INotificationClient>, INotificationServer
{
    private readonly List<long> subscribed = [];
    private APIActivity? activity;

    private readonly Valkey valkey;
    private IDisposable? onUserOnline;
    private IDisposable? onUserOffline;
    private IDisposable? onUserActivity;

    public NotificationSocket(Valkey valkey, IServiceProvider services)
        : base(services)
    {
        this.valkey = valkey;
    }

    protected override void OnOpen()
    {
        using (var scope = Services.CreateScope())
        {
            var database = UseDatabase(scope.ServiceProvider);
            var translator = scope.ServiceProvider.GetRequiredService<ModelTranslator>();
            var user = database.Users.Find(UserID);
            if (user is null) return; // TODO: force disconnect

            Client.Login(translator.ToAPI(user));
            if (user.ForceNameChange) Client.ForceNameChange();
        }

        onUserOnline = valkey.Subscribe<ValkeyIDEvent>(Valkey.UserOnline, e => updateFriendStatus(e.ID, true)).Result;
        onUserOffline = valkey.Subscribe<ValkeyIDEvent>(Valkey.UserOffline, e => updateFriendStatus(e.ID, false)).Result;
        onUserActivity = valkey.Subscribe<ValkeyIDEvent>(Valkey.UserActivity, e =>
        {
            if (!subscribed.Contains(e.ID))
                return;

            sendCurrentActivity(e.ID);
        }).Result;

        _ = valkey.Publish(Valkey.UserOnline, new ValkeyIDEvent(UserID));
        valkey.Database.SetAdd(Valkey.UsersOnline, UserID);
    }

    protected override void OnClose()
    {
        onUserOnline?.Dispose();
        onUserOffline?.Dispose();
        onUserActivity?.Dispose();
        subscribed.Clear();

        _ = valkey.Publish(Valkey.UserOffline, new ValkeyIDEvent(UserID));
        valkey.Database.SetRemove(Valkey.UsersOnline, UserID);
        ((INotificationServer)this).UpdateActivity("offline", new JObject());
    }

    private void updateFriendStatus(long id, bool state)
    {
        using var svc = Services.CreateScope();
        using var db = UseDatabase(svc.ServiceProvider);

        if (!db.UserFollows.Any(x => x.FollowerID == UserID && x.FolloweeID == id))
            return;

        var user = db.Users.Find(id);
        if (user is null) return;

        var translator = svc.ServiceProvider.GetRequiredService<ModelTranslator>();
        Client.NotifyFriendStatus(translator.ToAPI(user), state);
    }

    private void sendCurrentActivity(long id)
    {
        var val = valkey?.Database.StringGet($"{Valkey.USER_ACTIVITY}:{id}");
        if (val?.IsNull ?? true) return;

        var current = ((string)val!).Deserialize<APIActivity>();
        Client.NotifyUserActivity(id, current ?? APIActivity.Offline);
    }

    #region INotificationServer

    Task INotificationServer.UpdateActivity(string name, JObject data)
    {
        // TODO: validate
        activity = new APIActivity { Name = name, Data = data };

        var key = $"{Valkey.USER_ACTIVITY}:{UserID}";

        if (name == "offline") valkey.Database.KeyDelete(key);
        else valkey.Database.StringSet(key, activity.Serialize());

        return valkey.Publish(Valkey.UserActivity, new ValkeyIDEvent(UserID));
    }

    Task INotificationServer.SubscribeToUser(long id)
    {
        if (subscribed.Contains(id))
            return Task.CompletedTask;

        subscribed.Add(id);
        sendCurrentActivity(id);
        return Task.CompletedTask;
    }

    Task INotificationServer.UpdateNotificationUnread(long time)
    {
        // TODO: move this out of socket
        // why is this even here???
        return Task.CompletedTask;
    }

    #endregion
}
