/*
using System;
using System.Collections.Generic;
using fluxel.Components.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace fluxel.Services.Content.Tasks.Previews;

public class RegeneratePreviewsBulkTask : IBulkTask
{
    public IEnumerable<IBasicTask> GetTasks(IServiceProvider services)
        => services.GetRequiredService<MapManager>().AllSets.Select(set => new GeneratePreviewTask(set.ID));
}
*/
