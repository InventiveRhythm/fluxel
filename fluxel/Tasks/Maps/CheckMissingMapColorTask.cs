using System;
using System.Linq;
using System.Threading.Tasks;
using fluxel.Database;
using Microsoft.Extensions.DependencyInjection;

namespace fluxel.Tasks.Maps;

public class CheckMissingMapColorTask : IBasicTask
{
    public string Name => "CheckMissingMapColor";

    public Task Run(IServiceProvider services)
    {
        var mm = services.GetRequiredService<MapManager>();
        var tasks = services.GetRequiredService<TaskRunner>();

        foreach (var map in mm.AllMaps.Where(x => x.Color == 0))
            tasks.Schedule(new RefreshMapColorTask(map.ID));

        return Task.CompletedTask;
    }
}
