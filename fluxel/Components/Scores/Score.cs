using fluxel.Components.Maps;
using fluxel.Database;
using fluxel.Utils;
using Newtonsoft.Json;
using Realms;

namespace fluxel.Components.Scores;

public class Score : RealmObject {
    [PrimaryKey]
    [JsonProperty("id")]
    public int Id { get; set; }
    
    [JsonProperty("user")]
    public int UserId { get; set; }
    
    [JsonIgnore]
    public int MapId { get; set; }
    
    [Ignored]
    [JsonIgnore]
    public Map MapInfo => Map.FindById(MapId) ?? new Map();

    [Ignored]
    [JsonProperty("map")]
    public MapShort MapShort => Map.FindById(MapId)?.ToShort() ?? new MapShort();
    
    [JsonIgnore]
    public DateTimeOffset Time { get; set; } = DateTimeOffset.Now;

    [Ignored]
    [JsonProperty("time")]
    public long TimeLong => Time.ToUnixTimeSeconds();

    [Ignored]
    [JsonProperty("mode")]
    public int Mode => MapShort.Mode;
    
    [JsonProperty("mods")]
    public string Mods { get; set; } = "";

    [Ignored]
    [JsonProperty("pr")]
    public double PerformanceRating => MapShort.Rating + this.CalculatePerformanceRating();

    [Ignored]
    [JsonProperty("score")]
    public int TotalScore => this.CalculateScore();

    [Ignored]
    [JsonProperty("accuracy")]
    public float Accuracy => this.CalculateAccuracy();
    
    [Ignored]
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

    public static int GetNextId() {
        return RealmAccess.Run(realm => {
            var scores = realm.All<Score>();
            
            var max = 0;
            
            foreach (var score in scores) {
                if (score.Id > max) {
                    max = score.Id;
                }
            }
            
            return !scores.Any() ? 1 : max + 1;
        });
    }
}