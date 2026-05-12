using System;
using System.Threading.Tasks;
using fluxel.Components;
using fluxel.Database;
using Microsoft.Extensions.DependencyInjection;

namespace fluxel.Tasks.Users;

public class RecalculateUserTask : IBasicTask
{
    public string Name => $"RecalculateUser({id})";

    private long id { get; }

    public RecalculateUserTask(long id)
    {
        this.id = id;
    }

    public Task Run(IServiceProvider services)
    {
        var scores = services.GetRequiredService<ScoreManager>();
        var maps = services.GetRequiredService<MapManager>();
        var cache = services.GetRequiredService<RequestCache>();
        services.GetRequiredService<UserManager>().UpdateLocked(id, u => u.Recalculate(scores, maps, cache));
        return Task.CompletedTask;
    }
}
