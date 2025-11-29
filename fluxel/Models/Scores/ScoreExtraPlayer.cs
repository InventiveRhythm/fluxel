using System;
using System.Linq;
using fluxel.API.Components;
using fluxel.Database.Extensions;
using fluxel.Database.Helpers;
using fluxel.Models.Users;
using fluxel.Utils;
using fluXis.Online.API.Models.Users;
using fluXis.Scoring.Processing;
using fluXis.Utils;
using MongoDB.Bson.Serialization.Attributes;

namespace fluxel.Models.Scores;

public class ScoreExtraPlayer
{
    [BsonIgnore]
    public Score? Score { get; set; }

    [BsonElement("user")]
    public long UserID { get; init; }

    [BsonIgnore]
    public User? User => Cache.Users.Get(UserID) ?? UserHelper.Get(UserID);

    [BsonIgnore]
    public APIUser APIUser => User?.ToAPI() ?? APIUser.CreateUnknown(UserID);

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

    [BsonIgnore]
    public int JudgementCount => FlawlessCount + PerfectCount + GreatCount + AlrightCount + OkayCount + MissCount;

    [BsonElement("scrollspeed")]
    public float ScrollSpeed { get; set; }

    [BsonIgnore]
    public RequestCache Cache { get; set; } = new();

    public void Recalculate(int playerIndex)
    {
        if (Score is null)
        {
            throw new Exception("Score is null");
        }

        Accuracy = this.CalculateAccuracy();
        TotalScore = this.CalculateScore(playerIndex);
        PerformanceRating = ScoreProcessor.CalculatePerformance(
            (float)Score.Map.Rating,
            Accuracy,
            FlawlessCount,
            PerfectCount,
            GreatCount,
            AlrightCount,
            OkayCount,
            MissCount,
            Score.ModList.Select(ModUtils.GetFromAcronym).ToList()
        );
        Grade = this.GetGrade();
    }
}
