using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using fluxel.Database;
using fluxel.Tasks.Scores;
using fluxel.Tasks.Users;
using Microsoft.Extensions.DependencyInjection;

namespace fluxel.Tasks.Maps;

public class RefreshMapScoresTask : IBasicTask
{
    public string Name => "RefreshMapScores";

    public Task Run(IServiceProvider services)
    {
        var mm = services.GetRequiredService<MapManager>();
        var sm = services.GetRequiredService<ScoreManager>();
        var tasks = services.GetRequiredService<TaskRunner>();

        var maps = mm.NeedRefresh;
        var users = new List<long>();

        foreach (var map in maps)
        {
            var scores = sm.FromMap(map, map.SHA256Hash);

            foreach (var score in scores)
            {
                tasks.Schedule(new RecalculateScoreTask(score.ID));
                if (!users.Contains(score.UserID)) users.Add(score.UserID);
            }

            mm.QuickUpdate(map.ID, m => m.NeedsScoreRefresh = false);
        }

        foreach (var user in users)
        {
            tasks.Schedule(new RecalculateUserTask(user));
        }

        return Task.CompletedTask;
    }
}
