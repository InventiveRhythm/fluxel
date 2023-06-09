using fluxel.API;
using fluxel.Websocket;
using Newtonsoft.Json;

namespace fluxel;

public static class Program {
    public static async Task Main(string[] args) {
        Console.WriteLine("Starting fluxel...");
        
        ApiServer.Start();
        WebsocketServer.Start();
        
        Console.WriteLine("Ready!");
        await Task.Delay(-1);
    }
}