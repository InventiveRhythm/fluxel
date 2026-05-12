using System;
using System.Collections.Generic;
using System.Linq;
using fluxel.Database;
using Microsoft.Extensions.DependencyInjection;

namespace fluxel.Tasks.MapSets;

public class RegeneratePreviewsBulkTask : IBulkTask
{
    public IEnumerable<IBasicTask> GetTasks(IServiceProvider services)
        => services.GetRequiredService<MapManager>().AllSets.Select(set => new GeneratePreviewTask(set.ID));
}
