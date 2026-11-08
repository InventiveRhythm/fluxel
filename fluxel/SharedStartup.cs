using System;
using System.Collections.Generic;
using System.IO;
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
using Humanizer;
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
using osu.Framework.Extensions.TypeExtensions;
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

        builder.Services.ParseConfiguration<MailConfig>("MAIL");

        builder.Services.AssignSharedServices();
        builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(config.ValkeyConnection));
        builder.Services.AddSingleton<Valkey>();

        return (builder, config);
    }

    extension(IServiceCollection services)
    {
        public void AssignSharedServices()
        {
            services.AddScoped<Donations>();
            services.AddScoped<MailDelivery>();
            services.AddScoped<ModelTranslator>();
            services.AddSingleton<TaskRunner>();
            services.AddSingleton<UrlFormatter>();
            services.AddSingleton<GameModeManager, ServerGameModeManager>();
        }

        public void ParseConfiguration<T>(string prefix)
            where T : class, new()
        {
            services.Configure<T>(result =>
            {
                var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var property in properties)
                {
                    if (!property.CanWrite)
                        return;

                    var name = $"FLUXEL_{prefix.ToUpperInvariant()}_{property.Name.Underscore().ToUpper()}";
                    name = name.Replace("__", "_");
                    Logger.Log($"{property.Name} -> {name}");

                    var raw = Env.GetString(name);
                    if (string.IsNullOrWhiteSpace(raw)) continue;

                    var nullable = property.IsNullable();
                    var type = property.PropertyType.GetUnderlyingNullableType() ?? property.PropertyType;

                    switch (type.Name)
                    {
                        case nameof(String):
                            property.SetValue(result, raw);
                            break;

                        case nameof(Int32):
                            property.SetValue(result, int.Parse(raw));
                            break;

                        case nameof(Int64):
                            property.SetValue(result, long.Parse(raw));
                            break;

                        case nameof(UInt32):
                            property.SetValue(result, uint.Parse(raw));
                            break;

                        case nameof(UInt64):
                            property.SetValue(result, ulong.Parse(raw));
                            break;

                        default:
                            Logger.Log($"Unknown config type '{type.FullName}'!", level: LogLevel.Warning);
                            break;
                    }
                }
            });
        }

        public void SetupDatabase<T>(Action<DbContextOptionsBuilder> config)
            where T : DbContext
        {
            services.AddDbContext<T>(c =>
            {
                config.Invoke(c);

                if (!RuntimeUtils.IsDebugBuild)
                {
                    c.UseLoggerFactory(new NullLoggerFactory());
                    return;
                }

                c.EnableSensitiveDataLogging();
                c.EnableDetailedErrors();
            });
        }
    }

    extension(HostApplicationBuilder builder)
    {
        public void SetupPostgres()
        {
            builder.Services.SetupDatabase<DatabaseContext>(c =>
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
        if (RuntimeUtils.IsDebugBuild)
            Env.Load("docker/dev.env");

        if (File.Exists("docker/.env"))
            Env.Load("docker/.env");

        Env.Load();

        var config = new ServerConfig
        {
            Port = Env.GetInt("PORT", 2434),
            ValkeyConnection = Env.GetString("VALKEY", "valkey:6379"),
            MeilisearchUrl = Env.GetString("MEILI_URL", "http://meilisearch:7700"),
            MeilisearchKey = Env.GetString("MEILI_MASTER_KEY"),
            FfmpegPath = Env.GetString("FFMPEG_PATH", "ffmpeg"),
            KoFiSecret = Env.GetString("KOFI_SECRET"),
            BundledSets = [.. envLongList("BUNDLED_SETS")],
            Limits = new ServerConfig.LimitsConfig
            {
                MaxMapSets = Env.GetInt("LIMITS_MAX_MAPSETS"),
                IncreasePerPure = Env.GetInt("LIMITS_INCREASE_PER_PURE")
            },
            Urls = new ServerConfig.UrlConfig
            {
                Website = Env.GetString("URL_WEB"),
                Assets = Env.GetString("URL_ASSETS"),
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
