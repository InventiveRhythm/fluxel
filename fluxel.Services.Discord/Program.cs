using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace fluxel.Services.Discord;

internal abstract class Program
{
    internal static async Task Main(string[] args)
    {
        var (builder, config) = SharedStartup.CreateDefault();
        builder.Services.AddSingleton<DiscordBot>();
        var host = builder.Build();
        await host.RunAsync();
    }
}
