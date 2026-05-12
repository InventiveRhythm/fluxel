using System;
using System.Threading.Tasks;
using fluxel.Database;
using Microsoft.Extensions.DependencyInjection;

namespace fluxel.Tasks.Scores;

public class RecalculateScoreTask : IBasicTask
{
    public string Name => $"RecalculateScore({id})";

    private long id { get; }

    public RecalculateScoreTask(long id)
    {
        this.id = id;
    }

    public Task Run(IServiceProvider services)
    {
        var scores = services.GetRequiredService<ScoreManager>();
        var score = scores.Get(id);

        if (score == null)
            throw new ArgumentException($"No score with id {id} was found!");

        score.Recalculate(services.GetRequiredService<MapManager>());
        scores.Update(score);
        return Task.CompletedTask;
    }
}
