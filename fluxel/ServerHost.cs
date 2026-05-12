using System;
using fluxel.Tasks;
using Microsoft.Extensions.Logging;
using Midori.Logging;
using Midori.Networking;
using Midori.Utils;

namespace fluxel;

public class ServerHost
{
    public TaskRunner Scheduler { get; private set; } = null!;
    public HttpRouter Router { get; private set; } = null!;

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

                /*if (!debug)
                    DiscordBot.SendException(e);*/
            }
        };

        if (debug)
            return;

        /*Logger.Log("Setting up sentry...");

        SentrySdk.Init(opt =>
        {
            opt.Dsn = "https://1f54b9e0beadd0f97cad8a52c74cabce@sentry.flux.moe/5";
            opt.AutoSessionTracking = true;
            opt.Release = "current";
            opt.Environment = "server";
        });*/

        Logger.OnEntry += captureError;
    }

    private static void captureError(Logger.Entry entry)
    {
        if (entry.Level != LogLevel.Error)
            return;

        var ex = entry.Exception;

        if (ex == null)
            return;

        /*SentrySdk.CaptureEvent(new SentryEvent(ex)
        {
            Message = entry.Message,
            Level = SentryLevel.Error
        }, _ => { });*/
    }

    #endregion
}
