using System;
using System.Collections.Generic;
using System.Linq;
using fluxel.Components;
using fluxel.Database;
using fluxel.Tasks.Users;
using Microsoft.Extensions.DependencyInjection;
using Midori.Logging;

namespace fluxel.Tasks.Scores;

public class UpdateSetStatusBulkTask : IBulkTask
{
    private long id { get; }

    public UpdateSetStatusBulkTask(long id)
    {
        this.id = id;
    }

    public IEnumerable<IBasicTask> GetTasks(IServiceProvider services)
    {
        var set = services.GetRequiredService<MapManager>().GetSet(id);

        if (set is null)
            return Array.Empty<IBasicTask>();

        var scores = set.GetMaps(services.GetRequiredService<RequestCache>()).SelectMany(services.GetRequiredService<ScoreManager>().FromMap).ToList();
        var users = scores.Select(s => s.UserID).Distinct().ToList();
        Logger.Log($"Recalculating scores for {string.Join(", ", users)}. (status is {set.Status})");
        return users.Select(u => new RecalculateUserTask(u));
    }
}
