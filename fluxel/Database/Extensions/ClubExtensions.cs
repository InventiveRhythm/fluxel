using fluxel.Components;
using fluxel.Models.Clubs;

namespace fluxel.Database.Extensions;

public static class ClubExtensions
{
    public static long GetRank(this Club club, RequestCache cache)
    {
        if (club.OverallRating == 0)
            return 0;

        var all = cache.Clubs.All;
        all.Sort((a, b) => a.OverallRating.CompareTo(b.OverallRating));
        all.Reverse();

        var rank = 0;

        foreach (var c in all)
        {
            rank++;
            if (c.ID == club.ID) break;
        }

        return rank;
    }
}
