using System.Threading.Tasks;
using fluxel.Components.IPC;
using Microsoft.Extensions.Hosting;

namespace fluxel.Services.Beacon;

internal abstract class Beacon
{
    internal static async Task Main(string[] args)
    {
        var (builder, config) = SharedStartup.CreateDefault();
        builder.SetupAPI(config);
        builder.SetupPostgres();

        var host = builder.Build();
        host.ValkeyCleanup((string)Valkey.UsersOnline!);
        host.ValkeyCleanup($"{Valkey.USER_ACTIVITY}:*");
        host.RegisterModule<NotificationSocket>("/sockets/notifications");
        await host.RunAsync();
    }
}
