using WebSocketSharp.Server;

namespace fluxel.Websocket;

public static class WebsocketServer {
    public static void Start() {
        Console.WriteLine("Starting websocket...");

        const int port = 2435;
        var wss = new WebSocketServer(port);
        wss.AddWebSocketService<WebsocketConnection>("/");
        wss.Start();

        Console.WriteLine($"Websocket started on port {port}!");
    }
}
