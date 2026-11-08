using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace fluxel.Services.Admin;

internal abstract class Admin
{
    internal static async Task Main(string[] args)
    {
        var (builder, config) = SharedStartup.CreateDefault();
        builder.SetupAPI(config);
        builder.SetupPostgres();

        var host = builder.Build();
        host.RegisterControllers(typeof(Admin).Assembly);
        await host.RunAsync();
    }
}
