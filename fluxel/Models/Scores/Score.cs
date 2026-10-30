/*
using System;
using fluxel.Database;

namespace fluxel.Models.Scores;

[Obsolete]
public class LegacyScore
{
    public void Recalculate(MapManager maps)
    {
        var map = maps.GetMap(MapID);
        if (map is null) return;

        Accuracy = this.CalculateAccuracy();
        TotalScore = this.CalculateScore(map);
        PerformanceRating = ScoreProcessor.CalculatePerformance(
            (float)map.Rating,
            Accuracy,
            FlawlessCount,
            PerfectCount,
            GreatCount,
            AlrightCount,
            OkayCount,
            MissCount,
            ModList.Select(ModUtils.GetFromAcronym).ToList()
        );
        Grade = this.GetGrade();
    }
}

public enum ScoreIncludes
{
    Map
}
*/
