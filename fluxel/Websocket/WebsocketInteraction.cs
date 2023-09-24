using System.Net;
using Newtonsoft.Json;

namespace fluxel.Websocket;

public class WebsocketInteraction {
    private readonly WebsocketConnection connection;
    private readonly int id;

    public IPEndPoint RemoteEndPoint => connection.Address ?? throw new NullReferenceException();

    public Action<string> ReplyAction { get; init; } = _ => { };

    public WebsocketInteraction(WebsocketConnection connection, int id) {
        this.connection = connection;
        this.id = id;
    }

    public void Reply(int status = 200, string message = "OK!", object? data = null) {
        ReplyAction(JsonConvert.SerializeObject(new Dictionary<string, object?> {
            {"id", id},
            {"status", status},
            {"message", message},
            {"data", data}
        }));
    }
}
