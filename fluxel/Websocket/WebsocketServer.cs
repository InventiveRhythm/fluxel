using WebSocketSharp.Server;

namespace fluxel.Websocket;

public static class WebsocketServer {
    public static void Start() {
        Logger.Log("Starting websocket...");

        const int port = 2435;
        var wss = new WebSocketServer(port) { Log = { Output = (_, _) => { } } };
        wss.AddWebSocketService<WebsocketConnection>("/");
        wss.Start();

        Logger.Log($"Websocket started on port {port}!");
    }
}
