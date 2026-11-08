using System.Threading.Tasks;
using fluxel.Services.Chronicle.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace fluxel.Services.Chronicle;

internal abstract class Chronicle
{
    internal static async Task Main(string[] args)
    {
        var (builder, config) = SharedStartup.CreateDefault();
        builder.Services.AddSingleton<ScoreTickets>();
        builder.SetupAPI(config);
        builder.SetupPostgres();

        var host = builder.Build();
        host.RegisterControllers(typeof(Chronicle).Assembly);
        await host.RunAsync();
    }
}
