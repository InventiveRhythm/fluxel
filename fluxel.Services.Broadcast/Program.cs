using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Midori.Networking;

namespace fluxel.Services.Spectator;

internal abstract class Program
{
    internal static HttpConnectionManager<SpectatorSocket> SocketManager { get; private set; } = null!;

    internal static async Task Main()
    {
        var (builder, config) = SharedStartup.CreateDefault();
        builder.SetupAPI(config);
        builder.SetupPostgres();

        var host = builder.Build();
        SocketManager = host.RegisterModule<SpectatorSocket>("/sockets/spectator", manager: true)!;
        await host.RunAsync();
    }
}
