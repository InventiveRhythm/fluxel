using System;
using System.Collections.Generic;
using System.Linq;
using fluxel.Components;
using fluxel.Models.Scores;
using fluxel.Models.Users;
using fluxel.Models.Users.Equipment;

namespace fluxel.Database.Extensions;

public static class UserExtensions
{
    public static NamePaint? GetPaint(this User user, UserManager users)
    {
        if (string.IsNullOrEmpty(user.Paint))
            return null;

        return users.GetPaint(user.Paint);
    }

    public static bool IsDeveloper(this User user) => user.GroupIDs.Any(g => g == "dev");
    public static bool IsPurifier(this User user) => user.IsDeveloper() || user.GroupIDs.Any(g => g == "purifier");
    public static bool IsModerator(this User user) => user.IsDeveloper() || user.GroupIDs.Any(g => g == "moderators");

    public static List<Score> GetRecentScores(this User _, RequestCache cache, List<Score> scores, int? mode = null)
    {
        var maps = cache.Maps;
        var sets = cache.MapSets;
        maps.EnsureAll();
        sets.EnsureAll();

        var recent = new List<Score>();

        foreach (var score in scores.OrderByDescending(s => s.Time))
        {
            var map = maps.Get(score.MapID);

            if (map is null || !score.MatchesVersion(map) || !sets.TryGet(map.SetID, out var set))
                continue;

            if (mode > 0 && map.Mode != mode)
                continue;

            if (recent.Any(s => s.MapID == score.MapID) || !set.AllowScores())
                continue;

            recent.Add(score);

            if (recent.Count == 30)
                break;
        }

        return recent.ToList();
    }

    public static List<Score> GetBestScores(this User _, RequestCache cache, List<Score> scores, int? mode = null)
    {
        var maps = cache.Maps;
        var sets = cache.MapSets;
        maps.EnsureAll();
        sets.EnsureAll();

        var best = new List<Score>();

        foreach (var score in scores.OrderByDescending(s => s.PerformanceRating))
        {
            if (!maps.TryGet(score.MapID, out var map) || !score.MatchesVersion(map) || !sets.TryGet(map.SetID, out var set))
                continue;

            if (mode > 0 && map.Mode != mode)
                continue;

            if (best.Any(s => s.MapID == score.MapID) || !set.AllowScores())
                continue;

            best.Add(score);
        }

        return best.Take(50).ToList();
    }

    public static double CalculateOverallRating(List<Score> best)
    {
        var ovr = 0d;
        var count = 0;

        foreach (var score in best)
        {
            ovr += score.PerformanceRating * Math.Pow(.9f, count);
            count++;
        }

        return Math.Round(ovr, 2);
    }

    public static double CalculatePotentialRating(List<Score> best, List<Score> recent)
    {
        var b = best.Take(30).Sum(score => score.PerformanceRating);
        var r = recent.Take(10).Sum(score => score.PerformanceRating);
        return Math.Round((b + r) / 40f, 2);
    }

    public static double CalculateAccuracy(this User user, RequestCache cache, List<Score> scores)
    {
        double acc = 0;
        var count = 0;

        var maps = cache.Maps;
        var sets = cache.MapSets;
        maps.EnsureAll();
        sets.EnsureAll();

        foreach (var score in scores)
        {
            if (!maps.TryGet(score.MapID, out var map) || !score.MatchesVersion(map) || !sets.TryGet(map.SetID, out var set))
                continue;

            if (!set.AllowScores())
                continue;

            acc += Math.Round(score.Accuracy, 2);
            count++;
        }

        if (count == 0)
            return 0;

        return acc / count;
    }

    public static int CalculateMaxCombo(this User __, MapManager maps, List<Score> scores)
        => (from score in scores where maps.TryGetMap(score.MapID, out _) select score.MaxCombo).Prepend(0).Max();

    public static long CalculateRankedScore(this User __, MapManager maps, List<Score> scores)
        => scores.Where(score => maps.TryGetMap(score.MapID, out _)).Sum(score => (long)score.TotalScore);

    public static bool HasFlag(this User user, UserBanFlag banFlag) => (user.BanFlags & banFlag) == banFlag;

    public static UserBanFlag[] GetFlags(this User user) => Enum.GetValues(typeof(UserBanFlag)).Cast<UserBanFlag>().Where(user.HasFlag).ToArray();
}
