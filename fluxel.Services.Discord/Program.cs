using System.Threading;
using System.Threading.Tasks;
using fluxel.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace fluxel.Services.Discord;

internal abstract class Program
{
    internal static async Task Main(string[] args)
    {
        var (builder, config) = SharedStartup.CreateDefault();
        builder.Services.ParseConfiguration<DiscordConfig>("DISCORD");
        builder.Services.AddSingleton<DiscordBot>();

        var host = builder.Build();
        var bot = host.Services.GetRequiredService<DiscordBot>();

        await bot.StartAsync(CancellationToken.None);
        await host.RunAsync();
        await bot.StopAsync(CancellationToken.None);
    }
}
