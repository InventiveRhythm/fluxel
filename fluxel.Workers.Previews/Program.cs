using System;
using System.Threading;
using System.Threading.Tasks;
using fluxel.Components;
using fluxel.IPC;
using fluxel.Tasks;
using fluxel.Workers.Previews.API;
using fluxel.Workers.Previews.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Midori.Database.MongoDB;
using Midori.Networking;

namespace fluxel.Workers.Previews;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        var (builder, config) = SharedStartup.CreateDefault();
        builder.SetupAPI(config);

        builder.Services.AddMongoDatabase(config.Mongo.Connection, config.Mongo.Database);
        builder.Services.AddSingleton<PreviewGenerator>();

        var host = builder.Build();

        var router = host.Services.GetRequiredService<HttpRouter>();
        router.RegisterController<PreviewController>();

        var valkey = host.Services.GetRequiredService<Valkey>();
        var tasks = host.Services.GetRequiredService<TaskRunner>();

        await valkey.Subscribe<MapSetIDEvent>(Valkey.MapSetCreate, ev => tasks.Schedule(new GeneratePreviewTask(ev.ID)));
        await valkey.Subscribe<MapSetIDEvent>(Valkey.MapSetUpdate, ev => tasks.Schedule(new GeneratePreviewTask(ev.ID)));

        tasks.Schedule(new CheckForMissingPreviewsTask(), DateTime.Today, TimeSpan.FromDays(7));

        await tasks.StartAsync(CancellationToken.None);
        await host.RunAsync();

        await tasks.StopAsync(CancellationToken.None);
    }
}
