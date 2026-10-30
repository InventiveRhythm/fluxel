/*
using System;
using System.Collections.Generic;
using System.Linq;
using fluxel.Components.Tasks;
using fluxel.Database;
using Microsoft.Extensions.DependencyInjection;

namespace fluxel.Tasks.Clubs;

public class RefreshClubClaimsBulkTask : IBulkTask
{
    public IEnumerable<IBasicTask> GetTasks(IServiceProvider services)
        => services.GetRequiredService<MapManager>().AllMaps.Select(m => new RefreshClubClaimTask(m.ID));
}
*/
