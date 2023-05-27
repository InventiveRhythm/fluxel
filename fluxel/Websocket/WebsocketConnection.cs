using System.Net;
using fluxel.Websocket.Handlers.Account;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocketSharp;
using WebSocketSharp.Server;
using ErrorEventArgs = WebSocketSharp.ErrorEventArgs;

namespace fluxel.Websocket; 

public class WebsocketConnection : WebSocketBehavior {
    public IPEndPoint IP = null!;
    
    protected override void OnMessage(MessageEventArgs e) {
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
            _ => null
        };

        packetHandler?.Handle(new WebsocketInteraction(this, id) {
            ReplyAction = Send
        }, data);
    }

    protected override void OnClose(CloseEventArgs e) {
        Stats.RemoveOnlineUser(IP);
    }

    protected override void OnError(ErrorEventArgs e) {
        Console.WriteLine($"[{IP}] Error: {e.Message}!");
        Stats.RemoveOnlineUser(IP);
    }

    protected override void OnOpen() {
        IP = Context.UserEndPoint;
    } 
}