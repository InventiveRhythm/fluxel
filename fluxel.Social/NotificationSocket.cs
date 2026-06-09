using fluxel.Components;
using fluxel.Database;
using fluxel.Modules;
using fluxel.Modules.Messages;
using fluxel.WebSocket;
using fluXis.Online.API.Models.Users;
using fluXis.Online.Notifications;
using Newtonsoft.Json.Linq;

namespace fluxel.Social;

public class NotificationSocket : AuthenticatedSocket<INotificationServer, INotificationClient>, INotificationServer
{
    public List<long> SubscribedUsers { get; } = new();
    public APIActivity? Activity { get; private set; }

    private readonly ModelTranslator translator;
    private readonly NotificationsModule mod;
    private readonly ModuleManager modules;

    public NotificationSocket(NotificationsModule mod, UserManager users, ModuleManager modules, ModelTranslator translator)
        : base(users)
    {
        this.mod = mod;
        this.modules = modules;
        this.translator = translator;
    }

    protected override void OnOpen()
    {
        base.OnOpen();

        Client.Login(translator.ToAPI(Users.Get(UserID) ?? throw new ArgumentNullException(nameof(UserID), "how.")));
        modules.SendMessage(new UserOnlineStateMessage(UserID, true));

        if (CurrentUser.ForceNameChange)
            Client.ForceNameChange();
    }

    protected override void OnClose()
    {
        base.OnClose();

        SubscribedUsers.Clear();
        UpdateActivity("Offline", new JObject());

        modules.SendMessage(new UserOnlineStateMessage(UserID, false));
    }

    public Task UpdateActivity(string name, JObject data)
    {
        // we should really, REALLY validate this
        Activity = new APIActivity { Name = name, Data = data };

        foreach (var sock in mod.Sockets.Where(x => x.SubscribedUsers.Contains(UserID)))
        {
            _ = sock.Client.NotifyUserActivity(UserID, Activity);
        }

        return Task.CompletedTask;
    }

    public Task SubscribeToUser(long id)
    {
        if (SubscribedUsers.Contains(id))
            return Task.CompletedTask;

        SubscribedUsers.Add(id);

        var current = mod.Sockets.FirstOrDefault(x => x.UserID == id);
        _ = Client.NotifyUserActivity(id, current?.Activity ?? APIActivity.Online);
        return Task.CompletedTask;
    }

    public Task UpdateNotificationUnread(long time)
    {
        Users.UpdateLocked(UserID, x => x.LastNotificationRead = time);
        return Task.CompletedTask;
    }
}
