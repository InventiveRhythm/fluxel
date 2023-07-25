using System.Globalization;
using fluxel.Components.Scores;

namespace fluxel.Utils;

public static class ScoreUtils {
    public static int CalculateScore(this Score score) {
        var maxScore = (int)(1000000 * getMulitpliers(score));
        var accBased = (int)(score.Accuracy / 100f * (maxScore * .9f));
        var comboBased = (int)(score.MaxCombo / (float)score.MapInfo.MaxCombo * (maxScore * .1f));
        return accBased + comboBased;
    }

    public static float CalculateAccuracy(this Score score) {
        var rated = 0f;
        var total = score.FlawlessCount + score.PerfectCount + score.GreatCount + score.AlrightCount + score.OkayCount + score.MissCount;

        rated += score.FlawlessCount;
        rated += score.PerfectCount * .98f;
        rated += score.GreatCount * .65f;
        rated += score.AlrightCount * .25f;
        rated += score.OkayCount * .1f;

        return rated / total * 100f;
    }

    public static float CalculatePerformanceRating(this Score score) {
        var totalScore = score.TotalScore;

        if (totalScore > 1000000)
            return 2f + (totalScore - 1000000) / 200000f;

        if (totalScore >= 960000)
            return 1f + (totalScore - 960000) / 40000f;

        return (totalScore - 920000) / 80000f;
    }

    private static float getMulitpliers(this Score score) {
        var mods = score.Mods.Split(",");

        var multiplier = 1f;

        foreach (var mod in mods) {
            switch (mod) {
                case "EZ":
                    multiplier -= .3f;
                    break;

                case "HD":
                    multiplier += .04f;
                    break;

                case "NF":
                    multiplier -= .5f;
                    break;

                case "NLN":
                    multiplier -= .2f;
                    break;

                case "NSV":
                    multiplier -= .2f;
                    break;
            }

            if (mod.EndsWith("x"))
                multiplier += float.Parse(mod[..^1], NumberStyles.Float, CultureInfo.InvariantCulture) - 1f;
        }

        return multiplier;
    }

    public static string GetGrade(this Score score) {
        return score.Accuracy switch {
            100 => "X",
            >= 99 => "SS",
            >= 98 => "S",
            >= 95 => "AA",
            >= 90 => "A",
            >= 80 => "B",
            >= 70 => "C",
            _ => "D"
        };
    }
}
