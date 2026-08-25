using System.Net;
using DotNetEnv;
using fluxel.API;
using fluxel.API.Components;
using fluxel.Bot;
using fluxel.Components;
using fluxel.Database.Migrations;
using fluxel.Modules;
using fluxel.Tasks;
using fluxel.Tasks.Maps;
using fluxel.Tasks.Users.Connections;
using fluXis.Map;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Midori.Database.MongoDB;
using Midori.Networking;

namespace fluxel.Startup;

internal static class Program
{
    private static async Task Main()
    {
        osu.Framework.Logging.Logger.Enabled = false;

        MapInfo.MinKeymode = 1;
        MapInfo.MaxKeymode = 10;

        var (builder, config) = SharedStartup.CreateDefault();
        builder.Services.AddMongoDatabase(config.Mongo.Connection, config.Mongo.Database);
        builder.Services.AddSingleton<DatabaseMigrationRunner>();
        builder.SetupAPI(config);

        var modules = new ModuleManager();
        builder.Services.AddSingleton(_ => modules);
        modules.RegisterServices(builder);

        builder.Services.AddSingleton<ServerEvents>();
        builder.Services.AddSingleton<DiscordBot>();

        var host = builder.Build();
        modules.BuildModules(host.Services);

        var router = host.Services.GetRequiredService<HttpRouter>();
        router.AddMiddleware<FluxelAtMeMiddleware>();
        router.RegisterControllersFromAssembly(typeof(ServerHost).Assembly);
        modules.RegisterControllers(router);

        var discord = host.Services.GetRequiredService<DiscordBot>();
        var tasks = host.Services.GetRequiredService<TaskRunner>();

        tasks.Schedule(new CheckMissingMapColorTask(), DateTime.Today, TimeSpan.FromDays(1));
        tasks.Schedule(new RefreshMapScoresTask(), DateTime.Today, TimeSpan.FromDays(1));
        tasks.Schedule(new CheckForDiscordRefreshesTask(), DateTime.Today, TimeSpan.FromHours(8));

        await host.Services.GetRequiredService<DatabaseMigrationRunner>().ExecuteUpgrades();

        await tasks.StartAsync(CancellationToken.None);
        await discord.StartAsync(CancellationToken.None);

        await host.RunAsync();

        await tasks.StopAsync(CancellationToken.None);
        await discord.StopAsync(CancellationToken.None);
    }
}
