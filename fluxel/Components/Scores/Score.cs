using fluxel.Components.Maps;
using fluxel.Database.Helpers;
using fluxel.Utils;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace fluxel.Components.Scores;

public class Score {
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("user")]
    public long UserId { get; set; }

    [JsonIgnore]
    public long MapId { get; init; }

    [BsonIgnore]
    [JsonIgnore]
    public Map MapInfo => MapHelper.Get(MapId) ?? new Map();

    [BsonIgnore]
    [JsonProperty("map")]
    public MapShort MapShort => MapHelper.Get(MapId)?.ToShort() ?? new MapShort();

    [JsonIgnore]
    public DateTimeOffset Time { get; init; } = DateTimeOffset.Now;

    [BsonIgnore]
    [JsonProperty("time")]
    public long TimeLong => Time.ToUnixTimeSeconds();

    [BsonIgnore]
    [JsonProperty("mode")]
    public int Mode => MapShort.Mode;

    /// <summary>
    /// List of mods seperated by commas.
    /// </summary>
    [JsonProperty("mods")]
    public string Mods { get; set; } = "";

    [BsonIgnore]
    [JsonProperty("pr")]
    public double PerformanceRating => MapInfo.NotesPerSecond + this.CalculatePerformanceRating();

    [BsonIgnore]
    [JsonProperty("score")]
    public int TotalScore => this.CalculateScore();

    [BsonIgnore]
    [JsonProperty("accuracy")]
    public float Accuracy => this.CalculateAccuracy();

    [BsonIgnore]
    [JsonProperty("grade")]
    public string Grade => this.GetGrade();

    [JsonProperty("maxcombo")]
    public int MaxCombo { get; set; }

    [JsonProperty("flawless")]
    public int FlawlessCount { get; set; }

    [JsonProperty("perfect")]
    public int PerfectCount { get; set; }

    [JsonProperty("great")]
    public int GreatCount { get; set; }

    [JsonProperty("alright")]
    public int AlrightCount { get; set; }

    [JsonProperty("okay")]
    public int OkayCount { get; set; }

    [JsonProperty("miss")]
    public int MissCount { get; set; }

    [JsonProperty("scrollspeed")]
    public float ScrollSpeed { get; set; }
}
