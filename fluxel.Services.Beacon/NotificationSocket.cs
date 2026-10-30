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

        _ = valkey.Publish(Valkey.UserOnline, new ValkeyIDEvent(UserID));
        valkey.Database.SetAdd(Valkey.UsersOnline, UserID);
    }

    protected override void OnClose()
    {
        subscribed.Clear();
        _ = valkey.Publish(Valkey.UserOffline, new ValkeyIDEvent(UserID));
        valkey.Database.SetRemove(Valkey.UsersOnline, UserID);
        UpdateActivity("offline", new JObject());
    }

    public Task UpdateActivity(string name, JObject data)
    {
        // TODO: validate
        activity = new APIActivity { Name = name, Data = data };

        var key = $"{Valkey.USER_ACTIVITY}:{UserID}";

        if (name == "offline") valkey.Database.KeyDelete(key);
        else valkey.Database.StringSet(key, activity.Serialize());

        return Task.WhenAll(
            Program.SocketManager.Where(x => x.subscribed.Contains(UserID))
                   .Select(x => x.Client.NotifyUserActivity(UserID, activity))
        );
    }

    public async Task SubscribeToUser(long id)
    {
        if (subscribed.Contains(id))
            return;

        subscribed.Add(id);

        var current = Program.SocketManager.FirstOrDefault(x => x.UserID == id);
        await Client.NotifyUserActivity(id, current?.activity ?? APIActivity.Offline);
    }

    public Task UpdateNotificationUnread(long time)
    {
        // TODO: move this out of socket
        // why is this even here???
        return Task.CompletedTask;
    }
}
