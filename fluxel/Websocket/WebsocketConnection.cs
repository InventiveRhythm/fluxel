using System.Collections.Specialized;
using System.Net;
using fluxel.Websocket.Handlers.Account;
using fluxel.Websocket.Handlers.Chat;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocketSharp;
using WebSocketSharp.Server;
using ErrorEventArgs = WebSocketSharp.ErrorEventArgs;

namespace fluxel.Websocket; 

public class WebsocketConnection : WebSocketBehavior {
    public static readonly Dictionary<IPEndPoint, WebsocketConnection> Connections = new(); 

    public IPEndPoint IP = null!;
    
    protected override void OnMessage(MessageEventArgs e) {
        var json = JsonConvert.DeserializeObject<JObject>(e.Data);
        if (json == null) return;

        var id = json["id"]?.Value<int>() ?? -1;
        var data = json["data"];

        if (id == -1) return;

        if (data == null) return;

        Console.WriteLine($"[{IP}] Received packet {id}!");

        IPacketHandler? packetHandler = id switch {
            0 => new AuthHandler(),
            1 => new LoginHandler(),
            2 => new RegisterHandler(),
            10 => new ChatMessageHandler(),
            11 => new ChatHistoryHandler(),
            _ => null
        };

        packetHandler?.Handle(new WebsocketInteraction(this, id) {
            ReplyAction = Send
        }, data);
    }

    protected override void OnClose(CloseEventArgs e) {
        Stats.RemoveOnlineUser(IP);
        Connections.Remove(IP);
    }

    protected override void OnError(ErrorEventArgs e) {
        Console.WriteLine($"[{IP}] Error: {e.Message}!");
        Stats.RemoveOnlineUser(IP);
        Connections.Remove(IP);
    }

    protected override void OnOpen() {
        IP = Context.UserEndPoint;
        Connections.Add(IP, this);
        
        string?[] headerKeys = Context.Headers.AllKeys;
        
        foreach (var header in headerKeys) {
            string? value = Context.Headers[header];
            Console.WriteLine($"[{IP}] Header: {header} = {value}");
        }
    }
    
    public new void Send(string data) {
        base.Send(data);
    }
}