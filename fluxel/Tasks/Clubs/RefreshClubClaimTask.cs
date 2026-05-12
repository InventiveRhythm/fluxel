using System;
using System.Linq;
using System.Threading.Tasks;
using fluxel.Database;
using Microsoft.Extensions.DependencyInjection;

namespace fluxel.Tasks.Clubs;

public class RefreshClubClaimTask : IBasicTask
{
    public string Name => $"RefreshClubClaim({id})";

    private long id { get; }

    public RefreshClubClaimTask(long id)
    {
        this.id = id;
    }

    public Task Run(IServiceProvider services)
    {
        var clubs = services.GetRequiredService<ClubManager>();
        var map = services.GetRequiredService<MapManager>().GetMap(id);

        if (map == null)
            throw new ArgumentException($"No map with id {id} was found!");

        var claim = clubs.GetClaim(map.ID, true)!;

        var scores = clubs.GetScoresOnMap(map.ID);
        scores = scores.OrderByDescending(s => s.PerformanceRating).ToList();

        claim.ClubID = scores.FirstOrDefault()?.ClubID ?? 0;
        clubs.UpdateClaim(claim);
        return Task.CompletedTask;
    }
}
