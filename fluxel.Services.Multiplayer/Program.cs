using System.Threading.Tasks;
using fluxel.Services.Multiplayer.Lobby;
using Microsoft.Extensions.Hosting;
using Midori.Networking;

namespace fluxel.Services.Multiplayer;

internal abstract class Program
{
    public static HttpConnectionManager<MultiplayerSocket> SocketManager { get; private set; } = null!;

    internal static async Task Main(string[] args)
    {
        var (builder, config) = SharedStartup.CreateDefault();
        builder.SetupAPI(config);
        builder.SetupPostgres();

        var host = builder.Build();
        host.RegisterControllers(typeof(Program).Assembly);
        SocketManager = host.RegisterModule<MultiplayerSocket>("/sockets/multiplayer", manager: true)!;

        MultiplayerRoomManager.StartThread();
        await host.RunAsync();
        MultiplayerRoomManager.StopThread();
    }
}
