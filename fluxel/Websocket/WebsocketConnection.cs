using System.Net;
using fluxel.Websocket.Handlers.Account;
using fluxel.Websocket.Handlers.Chat;
using fluxel.Websocket.Handlers.Multiplayer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocketSharp;
using WebSocketSharp.Server;
using ErrorEventArgs = WebSocketSharp.ErrorEventArgs;

namespace fluxel.Websocket;

public class WebsocketConnection : WebSocketBehavior {
    public static readonly Dictionary<IPEndPoint, WebsocketConnection> CONNECTIONS = new();

    public IPEndPoint? Address { get; private set; }

    protected override void OnMessage(MessageEventArgs e) {
        if (Address == null)
            return;

        var json = JsonConvert.DeserializeObject<JObject>(e.Data);
        if (json == null) return;

        var id = json["id"]?.Value<int>() ?? -1;
        var data = json["data"];

        if (id == -1) return;

        if (data == null) return;

        IPacketHandler? packetHandler = id switch {
            0 => new AuthHandler(),
            1 => new LoginHandler(),
            2 => new RegisterHandler(),
            10 => new ChatMessageHandler(),
            11 => new ChatHistoryHandler(),
            12 => new ChatDeleteHandler(),
            21 => new MultiplayerJoinHandler(),
            22 => new MultiplayerLeaveHandler(),
            _ => null
        };

        packetHandler?.Handle(new WebsocketInteraction(this, id) {
            ReplyAction = Send
        }, data);
    }

    protected override void OnClose(CloseEventArgs e) {
        if (Address == null)
            return;

        GlobalStatistics.RemoveOnlineUser(Address);
        CONNECTIONS.Remove(Address);
    }

    protected override void OnError(ErrorEventArgs e) {
        if (Address == null)
            return;

        Logger.Log($"[{Address}] Error: {e.Message}!", LogLevel.Error);
        GlobalStatistics.RemoveOnlineUser(Address);
        CONNECTIONS.Remove(Address);
    }

    protected override void OnOpen() {
        var forwardedFor = Context.Headers["X-Forwarded-For"];

        if (forwardedFor != null) {
            var ips = forwardedFor.Split(',');
            Address = new IPEndPoint(IPAddress.Parse(ips[0]), Context.UserEndPoint.Port);
        }
        else {
            Logger.Log("X-Forwarded-For header not found, using remote endpoint", LogLevel.Warning);
            Address = Context.UserEndPoint;
        }

        CONNECTIONS.Add(Address, this);
    }

    public new void Send(string data) {
        base.Send(data);
    }
}
