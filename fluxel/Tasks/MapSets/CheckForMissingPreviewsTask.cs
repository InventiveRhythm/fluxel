using System;
using System.IO;
using System.Threading.Tasks;
using fluxel.Database;
using Microsoft.Extensions.DependencyInjection;

namespace fluxel.Tasks.MapSets;

public class CheckForMissingPreviewsTask : IBasicTask
{
    public string Name => "CheckForMissingPreviews";

    public Task Run(IServiceProvider services)
    {
        var mm = services.GetRequiredService<MapManager>();
        var tasks = services.GetRequiredService<TaskRunner>();

        foreach (var set in mm.AllPureSets)
        {
            var path = Assets.GetPathForAsset(AssetType.Preview, set.ID.ToString());
            if (!File.Exists(path)) tasks.Schedule(new GeneratePreviewTask(set.ID));
        }

        return Task.CompletedTask;
    }
}
