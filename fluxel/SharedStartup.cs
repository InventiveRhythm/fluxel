using System;
using System.Collections.Generic;
using System.Net;
using DotNetEnv;
using fluxel.API;
using fluxel.Components;
using fluxel.Config;
using fluxel.Database;
using fluxel.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Midori.API;
using Midori.API.Handlers;
using Midori.Logging;
using Midori.Networking.Handlers;
using Midori.Utils.Extensions;
using StackExchange.Redis;

namespace fluxel;

public static class SharedStartup
{
    public static (HostApplicationBuilder builder, ServerConfig config) CreateDefault()
    {
        var builder = new HostApplicationBuilder();
        var config = builder.setupConfig();

        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(new MidoriLoggerProvider());

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
        builder.Services.AddSingleton<RedemptionManager>();
        builder.Services.AddSingleton<ScoreManager>();
        builder.Services.AddSingleton<UserManager>();

        builder.Services.AddSingleton<Donations>();
        builder.Services.AddSingleton<MailDelivery>();
        builder.Services.AddScoped<ModelTranslator>();
        builder.Services.AddScoped<RequestCache>();
        builder.Services.AddSingleton<Statistics>();
        builder.Services.AddSingleton<TaskRunner>();
        builder.Services.AddSingleton<UrlFormatter>();

        builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(config.ValkeyConnection));
        builder.Services.AddSingleton<Valkey>();

        return (builder, config);
    }

    public static void SetupAPI(this HostApplicationBuilder builder, ServerConfig config)
    {
        builder.Services.AddSingleton<IHttpReplyHandler, DefaultAPIReplyHandler>();
        builder.Services.AddSingleton<IAPIAuthenticator, FluxelAPIAuthenticator>();

        builder.Services.AddHttpServer(c =>
        {
            c.Address = IPAddress.Loopback;
            c.Port = (ushort)config.Port;
        });
    }

    private static ServerConfig setupConfig(this HostApplicationBuilder builder)
    {
        Env.Load();

        var config = new ServerConfig
        {
            Port = Env.GetInt("PORT", 2434),
            ValkeyConnection = Env.GetString("VALKEY", "localhost:6379"),
            FfmpegPath = Env.GetString("FFMPEG_PATH", "ffmpeg"),
            KoFiSecret = Env.GetString("KOFI_SECRET"),
            BundledSets = [.. envLongList("BUNDLED_SETS")],
            Limits = new ServerConfig.LimitsConfig
            {
                MaxMapSets = Env.GetInt("LIMITS_MAX_MAPSETS"),
                IncreasePerPure = Env.GetInt("LIMITS_INCREASE_PER_PURE")
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
                ClientID = Env.GetString("DISCORD_CLIENT_ID"),
                ClientSecret = Env.GetString("DISCORD_CLIENT_SECRET"),
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

        builder.Services.AddSingleton<ServerConfig>(_ => config);
        return config;
    }

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
}
