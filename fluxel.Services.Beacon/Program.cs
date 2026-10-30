using System.Threading.Tasks;
using fluxel.Components.IPC;
using Microsoft.Extensions.Hosting;
using Midori.Networking;

namespace fluxel.Services.Beacon;

internal abstract class Program
{
    internal static HttpConnectionManager<NotificationSocket> SocketManager { get; private set; } = null!;

    internal static async Task Main(string[] args)
    {
        var (builder, config) = SharedStartup.CreateDefault();
        builder.SetupAPI(config);
        builder.SetupPostgres();

        var host = builder.Build();
        host.ValkeyCleanup((string)Valkey.UsersOnline!);
        host.ValkeyCleanup($"{Valkey.USER_ACTIVITY}:*");
        SocketManager = host.RegisterModule<NotificationSocket>("/sockets/notifications", manager: true)!;
        await host.RunAsync();
    }
}
