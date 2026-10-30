using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using DotNetEnv;
using fluxel.Components.Tasks;
using fluxel.Services.Content.Components;
using fluxel.Services.Content.Components.S3;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace fluxel.Services.Content;

internal abstract class Program
{
    internal static async Task Main(string[] args)
    {
        var (builder, config) = SharedStartup.CreateDefault();
        builder.SetupAPI(config);
        builder.SetupPostgres();

        builder.Services.AddSingleton<PreviewGenerator>();

        builder.Services.AddScoped<S3Helper>();
        builder.Services.AddSingleton(new AmazonS3Config
        {
            ServiceURL = Env.GetString("FLUXEL_S3_URL", "http://seaweedfs:8333"),
            ForcePathStyle = true
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
        // tasks.Schedule(new GeneratePreviewTask(1630));

        await tasks.StartAsync(CancellationToken.None);
        await host.RunAsync();
        await tasks.StopAsync(CancellationToken.None);
    }
}
