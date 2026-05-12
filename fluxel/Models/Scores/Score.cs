using System;
using System.Collections.Generic;
using System.Linq;
using fluxel.Database;
using fluxel.Utils;
using fluXis.Scoring.Processing;
using fluXis.Utils;
using MongoDB.Bson.Serialization.Attributes;

namespace fluxel.Models.Scores;

public class Score : IHasID
{
    [BsonId]
    public long ID { get; set; }

    [BsonElement("user")]
    public long UserID { get; init; }

    [BsonElement("map")]
    public long MapID { get; init; }

    /// <summary>
    /// Hash of the map when the score was submitted.
    /// </summary>
    [BsonElement("hash")]
    public string MapHash { get; init; } = string.Empty;

    [BsonElement("time")]
    public DateTimeOffset Time { get; init; } = DateTimeOffset.Now;

    [BsonIgnore]
    public long TimeLong => Time.ToUnixTimeSeconds();

    /// <summary>
    /// List of mods separated by commas.
    /// </summary>
    [BsonElement("mods")]
    public string Mods { get; init; } = "";

    [BsonIgnore]
    public List<string> ModList => Mods.Split(',').ToList();

    [BsonElement("pr")]
    public double PerformanceRating { get; set; }

    [BsonElement("score")]
    public int TotalScore { get; set; }

    [BsonElement("accuracy")]
    public float Accuracy { get; set; }

    [BsonElement("grade")]
    public string Grade { get; set; } = null!;

    [BsonElement("combo")]
    public int MaxCombo { get; set; }

    [BsonElement("flawless")]
    public int FlawlessCount { get; set; }

    [BsonElement("perfect")]
    public int PerfectCount { get; set; }

    [BsonElement("great")]
    public int GreatCount { get; set; }

    [BsonElement("alright")]
    public int AlrightCount { get; set; }

    [BsonElement("okay")]
    public int OkayCount { get; set; }

    [BsonElement("miss")]
    public int MissCount { get; set; }

    [BsonElement("scrollspeed")]
    public float ScrollSpeed { get; set; }

    [BsonElement("replay")]
    public bool HasReplay { get; set; }

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
