using System.Threading.Tasks;
using fluxel.Services.Multiplayer.Lobby;
using Microsoft.Extensions.Hosting;
using Midori.Networking;

namespace fluxel.Services.Multiplayer;

internal abstract class Multiplayer
{
    public static HttpConnectionManager<MultiplayerSocket> Sockets { get; private set; } = null!;

    internal static async Task Main(string[] args)
    {
        var (builder, config) = SharedStartup.CreateDefault();
        builder.SetupAPI(config);
        builder.SetupPostgres();

        var host = builder.Build();
        host.RegisterControllers(typeof(Multiplayer).Assembly);
        Sockets = host.RegisterModule<MultiplayerSocket>("/sockets/multiplayer", manager: true)!;

        MultiplayerRoomManager.StartThread();
        await host.RunAsync();
        MultiplayerRoomManager.StopThread();
    }
}
