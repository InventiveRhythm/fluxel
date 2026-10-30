using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace fluxel.Services.Atlas;

internal abstract class Program
{
    internal static async Task Main(string[] args)
    {
        var (builder, config) = SharedStartup.CreateDefault();
        builder.Services.AddSingleton<Meili>();
        builder.SetupAPI(config);
        builder.SetupPostgres();

        var host = builder.Build();
        host.RegisterControllers(typeof(Program).Assembly);
        await host.RunAsync();
    }
}
