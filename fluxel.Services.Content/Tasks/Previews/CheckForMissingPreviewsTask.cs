using System;
using System.Linq;
using System.Threading.Tasks;
using fluxel.Components.Tasks;
using fluxel.Models;
using fluxel.Services.Content.Components.S3;
using Microsoft.Extensions.DependencyInjection;

namespace fluxel.Services.Content.Tasks.Previews;

public class CheckForMissingPreviewsTask : IBasicTask
{
    public string Name => "CheckForMissingPreviews";

    public async Task Run(IServiceProvider services)
    {
        var database = services.GetRequiredService<DatabaseContext>();
        var tasks = services.GetRequiredService<TaskRunner>();
        var s3 = services.GetRequiredService<S3Helper>();

        foreach (var set in database.MapSets.Select(x => x.ID))
        {
            if (!await s3.FileExists("fluxel", $"previews/{set}.ogg"))
                tasks.Schedule(new GeneratePreviewTask(set));
        }
    }
}
