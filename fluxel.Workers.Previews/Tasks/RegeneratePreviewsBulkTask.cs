using System;
using System.Collections.Generic;
using System.Linq;
using fluxel.Database;
using fluxel.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace fluxel.Workers.Previews.Tasks;

public class RegeneratePreviewsBulkTask : IBulkTask
{
    public IEnumerable<IBasicTask> GetTasks(IServiceProvider services)
        => services.GetRequiredService<MapManager>().AllSets.Select(set => new GeneratePreviewTask(set.ID));
}
