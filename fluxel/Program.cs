using System.Net;
using fluxel.API;
using fluxel.Bot;
using fluxel.Database;
using fluxel.Multiplayer.OpenLobby;
using fluxel.Websocket;
using Newtonsoft.Json;

namespace fluxel;

public static class Program {
    public static Config Config { get; private set; } = null!;

    public static async Task Main(string[] args) {
        Logger.Log("Starting fluxel...");

        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) => {
            if (eventArgs.ExceptionObject is not Exception e)
                Logger.Log($"Unhandled exception: {eventArgs.ExceptionObject}", LogLevel.Error);
            else
                Logger.Log(e);
        };

        Logger.Log("Loading config...");

        var configJson = await File.ReadAllTextAsync("config.json");
        var json = JsonConvert.DeserializeObject<Config>(configJson);

        if (json == null) {
            Logger.Log("Config file is invalid!");
            return;
        }

        Config = json;

        Logger.Log("Setting up database...");
        MongoDatabase.Setup();

        if (args.Contains("--migrate")) {
            Logger.Log("Migrating database...");
        }

        ApiServer.Start();
        WebsocketServer.Start();
        DiscordBot.Start();

        Logger.Log("Adding test lobby...");
        LobbyHandler.AddLobby(new MultiLobby {
            RoomId = 1,
            Settings = new MultiLobbySettings {
                Name = "Test Lobby"
            },
            HostId = 0,
            Maps = new List<long> { 6 }
        });

        GlobalStatistics.AddOnlineUser(new IPEndPoint(IPAddress.Parse("0.0.0.0"), 0), 0);

        Logger.Log("Ready!");
        await Task.Delay(-1);
    }
}
