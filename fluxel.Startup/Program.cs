using System.Net;
using DotNetEnv;
using fluxel.API;
using fluxel.API.Components;
using fluxel.Bot;
using fluxel.Components;
using fluxel.Config;
using fluxel.Database;
using fluxel.Modules;
using fluxel.Tasks;
using fluxel.Tasks.Maps;
using fluXis.Map;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Midori.API;
using Midori.API.Handlers;
using Midori.Database.MongoDB;
using Midori.Logging;
using Midori.Networking;
using Midori.Networking.Handlers;
using Midori.Utils.Extensions;

namespace fluxel.Startup;

internal static class Program
{
    private static async Task Main()
    {
        Env.Load();

        var config = new ServerConfig
        {
            Port = Env.GetInt("PORT"),
            FfmpegPath = Env.GetString("FFMPEG_PATH"),
            KoFiSecret = Env.GetString("KOFI_SECRET"),
            BundledSets = envLongList("BUNDLED_SETS").ToArray(),
            Limits = new ServerConfig.LimitsConfig
            {
                MaxMapSets = Env.GetInt("LIMITS_MAX_MAPSETS"),
                IncreasePerPure = Env.GetInt("LIMITS_INCREASE_PER_PURE"),
                IncreasePerYear = Env.GetInt("LIMITS_INCREASE_PER_YEAR"),
                MaxDescChar = Env.GetInt("LIMITS_MAX_DESC_CHAR"),
            },
            Mongo = new ServerConfig.MongoConfig
            {
                Connection = Env.GetString("MONGO_CONN"),
                Database = Env.GetString("MONGO_DB"),
            },
            Steam = new ServerConfig.SteamConfig
            {
                AppID = (uint)Env.GetInt("STEAM_APPID"),
                WebKey = Env.GetString("STEAM_WEBKEY")
            },
            Urls = new ServerConfig.UrlConfig
            {
                Website = Env.GetString("URL_WEB"),
                Assets = Env.GetString("URL_ASSETS"),
            },
            Discord = new ServerConfig.DiscordConfig
            {
                Token = Env.GetString("DISCORD_TOKEN"),
                Logging = envULong("DISCORD_LOGGING"),
                Registrations = envULong("DISCORD_REGISTER"),
                MapSubmissions = envULong("DISCORD_SUBMISSIONS"),
                MapFirstPlace = envULong("DISCORD_FIRST_PLACE"),
                MapRanked = envULong("DISCORD_RANKED"),
                QueueUpdates = envULong("DISCORD_QUEUE"),
                ChatLink = envULong("DISCORD_CHAT_LINK")
            },
            Mail = new ServerConfig.MailConfig
            {
                Host = Env.GetString("MAIL_HOST"),
                Port = Env.GetInt("MAIL_PORT"),
                Username = Env.GetString("MAIL_USER"),
                Password = Env.GetString("MAIL_PASS"),
                Name = Env.GetString("MAIL_NAME")
            },
            MailFlux = new ServerConfig.MailConfig
            {
                Host = Env.GetString("MAIL_DONO_HOST"),
                Port = Env.GetInt("MAIL_DONO_PORT"),
                Username = Env.GetString("MAIL_DONO_USER"),
                Password = Env.GetString("MAIL_DONO_PASS"),
                Name = Env.GetString("MAIL_DONO_NAME")
            }
        };

        osu.Framework.Logging.Logger.Enabled = false;

        MapInfo.MinKeymode = 1;
        MapInfo.MaxKeymode = 10;

        var builder = new HostApplicationBuilder();

        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(new MidoriLoggerProvider());

        builder.Services.AddSingleton<ServerConfig>(_ => config);

        builder.Services.AddSingleton<IHttpReplyHandler, DefaultAPIReplyHandler>();
        builder.Services.AddSingleton<IAPIAuthenticator, FluxelAPIAuthenticator>();
        builder.Services.AddScoped<RequestCache>();

        builder.Services.AddMongoDatabase(config.Mongo.Connection, config.Mongo.Database);
        builder.Services.AddSingleton<AchievementManager>();
        builder.Services.AddSingleton<ArtistManager>();
        builder.Services.AddSingleton<AuthManager>();
        builder.Services.AddSingleton<ChatManager>();
        builder.Services.AddSingleton<ClubManager>();
        builder.Services.AddSingleton<CollectionManager>();
        builder.Services.AddSingleton<CounterManager>();
        builder.Services.AddSingleton<EventManager>();
        builder.Services.AddSingleton<GroupManager>();
        builder.Services.AddSingleton<MapManager>();
        builder.Services.AddSingleton<NotificationManager>();
        builder.Services.AddSingleton<OAuthManager>();
        builder.Services.AddSingleton<ScoreManager>();
        builder.Services.AddSingleton<UserManager>();

        builder.Services.AddSingleton<DiscordBot>();
        builder.Services.AddSingleton<Donations>();
        builder.Services.AddSingleton<MailDelivery>();
        builder.Services.AddScoped<ModelTranslator>();
        builder.Services.AddSingleton<PreviewGenerator>();
        builder.Services.AddSingleton<ServerEvents>();
        builder.Services.AddSingleton<Statistics>();
        builder.Services.AddSingleton<TaskRunner>();
        builder.Services.AddSingleton<UrlFormatter>();

        builder.Services.AddHttpServer(c =>
        {
            c.Address = IPAddress.Loopback;
            c.Port = (ushort)config.Port;
        });

        var modules = new ModuleManager();
        builder.Services.AddSingleton(_ => modules);
        modules.RegisterServices(builder);

        var host = builder.Build();
        modules.BuildModules(host.Services);

        var router = host.Services.GetRequiredService<HttpRouter>();
        router.AddMiddleware<FluxelAtMeMiddleware>();
        router.RegisterControllersFromAssembly(typeof(ServerHost).Assembly);
        modules.RegisterControllers(router);

        var discord = host.Services.GetRequiredService<DiscordBot>();
        var tasks = host.Services.GetRequiredService<TaskRunner>();

        tasks.Schedule(new RefreshMapScoresTask(), DateTime.Today, TimeSpan.FromDays(1));

        await tasks.StartAsync(CancellationToken.None);
        await discord.StartAsync(CancellationToken.None);

        await host.RunAsync();

        await tasks.StopAsync(CancellationToken.None);
        await discord.StopAsync(CancellationToken.None);
    }

    #region Env

    private static ulong envULong(string key)
    {
        var var = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrWhiteSpace(var)) return 0;

        if (ulong.TryParse(var, out var ul))
            return ul;

        return 0;
    }

    private static List<long> envLongList(string key)
    {
        var var = Env.GetString(key);
        if (string.IsNullOrWhiteSpace(var)) return [];

        var split = var.Split(",", StringSplitOptions.RemoveEmptyEntries);
        var nums = new List<long>();

        foreach (var se in split)
        {
            if (!long.TryParse(se, out var n))
                return [];

            nums.Add(n);
        }

        return nums;
    }

    #endregion
}
