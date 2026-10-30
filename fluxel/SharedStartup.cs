using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using DotNetEnv;
using fluxel.API;
using fluxel.Components;
using fluxel.Components.IPC;
using fluxel.Components.Tasks;
using fluxel.Config;
using fluxel.Models;
using fluXis.Modes;
using fluXis.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Midori.API;
using Midori.API.Handlers;
using Midori.Logging;
using Midori.Networking;
using Midori.Networking.Handlers;
using Midori.Utils;
using Midori.Utils.Extensions;
using StackExchange.Redis;

namespace fluxel;

public static class SharedStartup
{
    public static (HostApplicationBuilder builder, ServerConfig config) CreateDefault()
    {
        JsonUtils.Converters.Add(new JsonResourceLocationConverter());

        var builder = new HostApplicationBuilder();
        var config = Assets.Config = builder.setupConfig();

        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(new MidoriLoggerProvider());

        builder.Services.AddScoped<Donations>();
        builder.Services.AddScoped<MailDelivery>();
        builder.Services.AddScoped<ModelTranslator>();
        builder.Services.AddSingleton<TaskRunner>();
        builder.Services.AddSingleton<UrlFormatter>();
        builder.Services.AddSingleton<GameModeManager, ServerGameModeManager>();

        builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(config.ValkeyConnection));
        builder.Services.AddSingleton<Valkey>();

        return (builder, config);
    }

    extension(HostApplicationBuilder builder)
    {
        public void SetupDatabase<T>(Action<DbContextOptionsBuilder> config)
            where T : DbContext
        {
            builder.Services.AddDbContext<T>(c =>
            {
                config.Invoke(c);

                if (!builder.Environment.IsDevelopment())
                {
                    c.UseLoggerFactory(new NullLoggerFactory());
                    return;
                }

                c.EnableSensitiveDataLogging();
                c.EnableDetailedErrors();
            });
        }

        public void SetupPostgres()
        {
            builder.SetupDatabase<DatabaseContext>(c =>
            {
                c.UseNpgsql(
                    DatabaseContext.BuildConnectionString(x => Env.GetString(x)),
                    o => o.ConfigureDataSource(d => d.EnableDynamicJson())
                );
            });
        }

        public void SetupAPI(ServerConfig config)
        {
            builder.Services.AddSingleton<IHttpReplyHandler, DefaultAPIReplyHandler>();
            builder.Services.AddScoped<IAPIAuthenticator, FluxelAPIAuthenticator>();

            builder.Services.AddHttpServer(c =>
            {
                c.Address = IPAddress.Any;
                c.Port = (ushort)config.Port;
            });
        }
    }

    extension(IHost host)
    {
        public HttpConnectionManager<T>? RegisterModule<T>(string path, bool manager = false)
            where T : class, IHttpModule
        {
            var router = host.Services.GetRequiredService<HttpRouter>();
            router.AddMiddleware<FluxelAtMeMiddleware>();
            return router.MapModule<T>(path, manager: manager);
        }

        public void RegisterControllers(Assembly assembly)
        {
            var router = host.Services.GetRequiredService<HttpRouter>();
            router.AddMiddleware<FluxelAtMeMiddleware>();
            router.RegisterControllersFromAssembly(assembly);
        }

        public void ValkeyCleanup(RedisValue pattern)
        {
            var valkey = host.Services.GetRequiredService<Valkey>();
            valkey.Cleanup(pattern);
        }
    }

    private static ServerConfig setupConfig(this HostApplicationBuilder builder)
    {
        Env.Load();

        var config = new ServerConfig
        {
            Port = Env.GetInt("PORT", 2434),
            AssetsPath = Env.GetString("ASSETS_PATH", "/srv/assets"),
            ValkeyConnection = Env.GetString("VALKEY", "localhost:6379"),
            MeilisearchUrl = Env.GetString("MEILI_URL", "http://localhost:7700"),
            MeilisearchKey = Env.GetString("MEILI_MASTER_KEY"),
            FfmpegPath = Env.GetString("FFMPEG_PATH", "ffmpeg"),
            KoFiSecret = Env.GetString("KOFI_SECRET"),
            BundledSets = [.. envLongList("BUNDLED_SETS")],
            Limits = new ServerConfig.LimitsConfig
            {
                MaxMapSets = Env.GetInt("LIMITS_MAX_MAPSETS"),
                IncreasePerPure = Env.GetInt("LIMITS_INCREASE_PER_PURE")
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
