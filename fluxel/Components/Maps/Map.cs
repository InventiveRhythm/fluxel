using fluxel.Components.Users;
using fluxel.Database.Helpers;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace fluxel.Components.Maps;

public class Map {
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("mapset")]
    public long SetId { get; set; }

    [JsonProperty("hash")]
    public string Hash { get; set; } = "";

    [JsonIgnore]
    public long MapperId { get; set; }

    [BsonIgnore]
    [JsonProperty("mapper")]
    public UserShort Mapper => UserHelper.Get(MapperId)?.ToShort() ?? new UserShort();

    [JsonProperty("difficulty")]
    public string Difficulty { get; set; } = "";

    [JsonProperty("mode")]
    public int Mode { get; set; }

    [JsonProperty("title")]
    public string Title { get; set; } = "";

    [JsonProperty("artist")]
    public string Artist { get; set; } = "";

    [JsonProperty("source")]
    public string Source { get; set; } = "";

    [JsonProperty("tags")]
    public string Tags { get; set; } = "";

    [JsonProperty("bpm")]
    public double Bpm { get; set; }

    [JsonProperty("length")]
    public int Length { get; set; }

    [JsonProperty("rating")]
    public double Rating { get; set; }

    [JsonProperty("hits")]
    public int Hits { get; set; }

    [JsonProperty("lns")]
    public int LongNotes { get; set; }

    [BsonIgnore]
    [JsonProperty("maxcombo")]
    public int MaxCombo => Hits + LongNotes * 2;

    [BsonIgnore]
    [JsonProperty("nps")]
    public double NotesPerSecond => Math.Round((Hits + LongNotes * 2) / (double)(Length / 1000f), 2);

    public MapShort ToShort() {
        return new MapShort {
            Id = Id,
            MapSet = SetId,
            Hash = Hash,
            Title = Title,
            Artist = Artist,
            Difficulty = Difficulty,
            Mode = Mode,
            Rating = Rating,
            Status = MapSetHelper.Get(SetId)?.Status ?? 0,
            MapperId = MapperId
        };
    }
}
