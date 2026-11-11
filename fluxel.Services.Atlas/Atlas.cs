using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace fluxel.Services.Atlas;

internal abstract class Atlas
{
    internal static async Task Main(string[] args)
    {
        var (builder, config) = SharedStartup.CreateDefault();
        builder.Services.AddSingleton<Meili>();
        builder.SetupAPI(config);
        builder.SetupPostgres();

        var host = builder.Build();
        host.RegisterControllers(typeof(Atlas).Assembly);

        // init valkey listeners
        _ = host.Services.GetRequiredService<Meili>();

        await host.RunAsync();
    }
}
