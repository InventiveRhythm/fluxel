using System;
using System.Linq;
using System.Threading.Tasks;
using fluxel.Components.Tasks;
using fluxel.Models;
using fluxel.Services.Content.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace fluxel.Services.Content.Tasks.Maps;

public class RecalculateMapRatingTask : IBasicTask
{
    public string Name => $"{nameof(RecalculateMapRatingTask)}({id})";

    private readonly long id;

    public RecalculateMapRatingTask(long id)
    {
        this.id = id;
    }

    public Task Run(IServiceProvider services)
    {
        var database = services.GetRequiredService<DatabaseContext>();
        var map = database.Maps.Include(x => x.MapSet).FirstOrDefault(x => x.ID == id);
        if (map is null) return Task.CompletedTask;

        var states = database.MapUserStates.Where(x => x.MapID == id && x.RateChart != null).ToArray();

        using (database.EditAndSave())
        {
            map.CalculateRating(states, out var refresh);
            map.HasPendingScoreRefresh |= refresh;
        }

        return Task.CompletedTask;
    }
}
