using fluxel.Components;
using fluxel.Database;
using fluxel.Modules;
using fluxel.Modules.Messages;
using fluxel.WebSocket;
using fluXis.Online.Notifications;
using Newtonsoft.Json.Linq;

namespace fluxel.Social;

public class NotificationSocket : AuthenticatedSocket<INotificationServer, INotificationClient>, INotificationServer
{
    public (string name, JObject data)? Activity { get; private set; }

    private readonly ModelTranslator translator;
    private readonly ModuleManager modules;

    public NotificationSocket(UserManager users, ModuleManager modules, ModelTranslator translator)
        : base(users)
    {
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
        modules.SendMessage(new UserOnlineStateMessage(UserID, false));
    }

    public Task UpdateActivity(string name, JObject data)
    {
        Activity = (name, data);
        return Task.CompletedTask;
    }

    public Task UpdateNotificationUnread(long time)
    {
        Users.UpdateLocked(UserID, x => x.LastNotificationRead = time);
        return Task.CompletedTask;
    }
}
