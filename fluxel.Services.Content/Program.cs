using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using fluxel.Components.IPC;
using fluxel.Components.Tasks;
using fluxel.Services.Content.Components;
using fluxel.Services.Content.Components.S3;
using fluxel.Services.Content.Tasks.Previews;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace fluxel.Services.Content;

internal abstract class Program
{
    internal static async Task Main(string[] args)
    {
        var (builder, config) = SharedStartup.CreateDefault();
        builder.Services.ParseConfiguration<S3Config>("S3");
        builder.SetupAPI(config);
        builder.SetupPostgres();

        builder.Services.AddSingleton<PreviewGenerator>();

        builder.Services.AddScoped<S3Helper>();
        builder.Services.AddSingleton(c =>
        {
            var cfg = c.GetRequiredService<IOptions<S3Config>>();
            return new AmazonS3Config { ServiceURL = cfg.Value.Url, ForcePathStyle = true };
        });

        var host = builder.Build();
        host.RegisterControllers(typeof(Program).Assembly);

        using (var scope = host.Services.CreateScope())
        {
            var s3 = scope.ServiceProvider.GetRequiredService<S3Helper>();
            await s3.EnsureBucketExists("uploads", new LifecycleRule
            {
                Id = "DeleteAfterOneDay",
                Status = LifecycleRuleStatus.Enabled,
                Expiration = new LifecycleRuleExpiration { Days = 1 }
            });
        }

        var tasks = host.Services.GetRequiredService<TaskRunner>();
        tasks.Schedule(new CheckForMissingPreviewsTask(), DateTime.Today, TimeSpan.FromDays(1));

        var valkey = host.Services.GetRequiredService<Valkey>();
        await valkey.Subscribe<ValkeyIDEvent>(Valkey.MapSetCreate, ev => tasks.Schedule(new GeneratePreviewTask(ev.ID)));
        await valkey.Subscribe<ValkeyIDEvent>(Valkey.MapSetUpdate, ev => tasks.Schedule(new GeneratePreviewTask(ev.ID)));

        await tasks.StartAsync(CancellationToken.None);
        await host.RunAsync();
        await tasks.StopAsync(CancellationToken.None);
    }
}
