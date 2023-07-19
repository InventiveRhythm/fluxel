using fluxel.API;
using fluxel.Multiplayer.OpenLobby;
using fluxel.Websocket;
using Newtonsoft.Json;

namespace fluxel;

public static class Program {
    public static async Task Main(string[] args) {
        Console.WriteLine("Starting fluxel...");
        
        ApiServer.Start();
        WebsocketServer.Start();
        
        LobbyHandler.AddLobby(new MultiLobby {
            RoomId = 1,
            Settings = new MultiLobbySettings {
                Name = "Test Lobby"
            },
            HostId = 0,
            Maps = new List<int> { 1 }
        });
        
        Console.WriteLine("Ready!");
        await Task.Delay(-1);
    }
}