using fluxel.API;
using fluxel.Multiplayer.OpenLobby;
using fluxel.Websocket;

namespace fluxel;

public static class Program {
    public static async Task Main() {
        Logger.Log("Starting fluxel...");

        ApiServer.Start();
        WebsocketServer.Start();

        LobbyHandler.AddLobby(new MultiLobby {
            RoomId = 1,
            Settings = new MultiLobbySettings {
                Name = "Test Lobby"
            },
            HostId = 0,
            Maps = new List<int> { 6 }
        });

        Logger.Log("Ready!");
        await Task.Delay(-1);
    }
}
