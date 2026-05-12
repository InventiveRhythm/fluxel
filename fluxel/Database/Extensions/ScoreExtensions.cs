using fluxel.Models.Maps;
using fluxel.Models.Scores;

namespace fluxel.Database.Extensions;

public static class ScoreExtensions
{
    public static bool MatchesVersion(this Score score, Map map) => score.MatchesVersion(map.SHA256Hash);
    public static bool MatchesVersion(this Score score, string? hash) => score.MapHash == hash;
}
