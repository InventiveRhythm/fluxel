using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using fluxel.API;
using fluxel.API.Components;
using fluxel.Bot;
using fluxel.Config;
using fluxel.Database;
using fluxel.Database.Helpers;
using fluxel.Models;
using fluxel.Modules;
using fluxel.Tasks;
using fluXis.Map;
using Midori.API;
using Midori.Logging;
using Midori.Networking;
using Midori.Utils;
using Sentry;

namespace fluxel;

public class ServerHost
{
    public static ServerHost Instance { get; private set; } = null!;
    public static ServerConfig Configuration => Instance.config;

    public TaskRunner Scheduler { get; private set; } = null!;
    public HttpServer Server { get; private set; } = null!;

    public IOnlineStateManager? OnlineStates { get; private set; }
    public IMultiRoomManager? MultiplayerRooms { get; private set; }

    private readonly ServerConfig config;
    private readonly List<IModule> modules = new();

    public ServerHost(ServerConfig config)
    {
        Instance = this;
        this.config = config;
    }

    public async Task StartBlocking()
    {
        osu.Framework.Logging.Logger.Enabled = false;

        MapInfo.MinKeymode = 4;
        MapInfo.MaxKeymode = 8;

        setupErrorLogging();
        setupDatabase();

        Scheduler = new TaskRunner();
        Server = new HttpServer
        {
            NotFoundModule = new APIRouteModule<FluxelAPIInteraction, NotFoundRoute>()
        };

        Server.RegisterAPI<FluxelAPIInteraction, IFluxelAPIRoute>(typeof(ServerHost).Assembly);

        loadModules();

        Server.Start(IPAddress.Loopback, config.Port);
        Scheduler.Start();

        await DiscordBot.StartAsync(config.Discord);

        Logger.Log("Ready!");
        await Task.Delay(-1);
    }

    #region Error Logging

    private void setupErrorLogging()
    {
        var debug = RuntimeUtils.IsDebugBuild;

        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
        {
            if (eventArgs.ExceptionObject is not Exception e)
                Logger.Log($"Unknown exception occurred! {eventArgs.ExceptionObject}", LoggingTarget.General, LogLevel.Error);
            else
            {
                Logger.Error(e, "Unhandled exception occurred!");

                if (!debug)
                    DiscordBot.SendException(e);
            }
        };

        if (debug)
            return;

        Logger.Log("Setting up sentry...");

        SentrySdk.Init(opt =>
        {
            opt.Dsn = "https://1f54b9e0beadd0f97cad8a52c74cabce@sentry.flux.moe/5";
            opt.AutoSessionTracking = true;
            opt.Release = "current";
            opt.Environment = "server";
        });

        Logger.OnEntry += captureError;
    }

    private static void captureError(Logger.Entry entry)
    {
        if (entry.Level != LogLevel.Error)
            return;

        var ex = entry.Exception;

        if (ex == null)
            return;

        SentrySdk.CaptureEvent(new SentryEvent(ex)
        {
            Message = entry.Message,
            Level = SentryLevel.Error
        }, _ => { });
    }

    #endregion

    #region Database

    private void setupDatabase()
    {
        Logger.Log("Setting up database...");
        MongoDatabase.Setup(config.Mongo);

        setupDefaultData();
    }

    private static void setupDefaultData()
    {
        CounterHelper.Add(CounterType.Club, () => ClubHelper.All.LastOrDefault()?.ID ?? 0);
        CounterHelper.Add(CounterType.Map, () => MapHelper.All.LastOrDefault()?.ID ?? 0);
        CounterHelper.Add(CounterType.MapSet, () => MapSetHelper.All.LastOrDefault()?.ID ?? 0);
        CounterHelper.Add(CounterType.Score, () => ScoreHelper.All.LastOrDefault()?.ID ?? 0);
        CounterHelper.Add(CounterType.User, () => UserHelper.All.LastOrDefault()?.ID ?? 0);
    }

    #endregion

    #region Modules

    private void loadModules()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var name = assembly.GetName().Name;
            if (name == null) continue;
            if (!name.StartsWith("fluxel", StringComparison.InvariantCultureIgnoreCase)) continue;

            loadModule(assembly);
        }

        var path = Path.GetDirectoryName(typeof(ServerHost).Assembly.Location)!;
        string[] files = Directory.GetFiles(path, "fluxel.*.dll");

        foreach (var file in files)
        {
            try
            {
                var assembly = Assembly.LoadFrom(file);
                loadModule(assembly);
            }
            catch (Exception e)
            {
                Logger.Error(e, $"Failed to load module {file} from directory!");
            }
        }
    }

    private void loadModule(Assembly assembly)
    {
        string name = assembly.GetName().Name!;

        try
        {
            var types = assembly.GetTypes().Where(t => t.GetInterfaces().Contains(typeof(IModule)));

            foreach (var type in types)
            {
                if (Activator.CreateInstance(type) is not IModule mod)
                {
                    Logger.Log($"Failed to load module {name}!");
                    continue;
                }

                mod.OnLoad(this);
                Server.RegisterAPI<FluxelAPIInteraction, IFluxelAPIRoute>(assembly);
                modules.Add(mod);

                OnlineStates ??= mod as IOnlineStateManager;
                MultiplayerRooms ??= mod as IMultiRoomManager;

                Logger.Log($"Loaded module {mod}!");
            }
        }
        catch (Exception e)
        {
            Logger.Error(e, $"Failed to load module {name}!");
        }
    }

    public void SendMessage(object data)
    {
        modules.ForEach(x =>
        {
            try
            {
                x.OnMessage(data);
            }
            catch (Exception ex)
            {
            }
        });
    }

    #endregion
}
