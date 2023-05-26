using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace fluxel.Websocket; 

public class WebsocketInteraction {
    private WebsocketConnection _connection;
    private readonly int _id;
    
    public Action<string> ReplyAction { get; init; } = _ => { };

    public WebsocketInteraction(WebsocketConnection connection, int id) {
        _connection = connection;
        _id = id;
    }
    
    public void Reply(int status = 200, string message = "OK!", object? data = null) {
        ReplyAction(JsonConvert.SerializeObject(new Dictionary<string, object?> {
            {"id", _id},
            {"status", status},
            {"message", message},
            {"data", data}
        }));
    }
}