using fluxel.Components.Users;
using fluxel.Database.Helpers;
using Newtonsoft.Json;

namespace fluxel.Components.Maps;

public class MapShort {
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("mapset")]
    public long MapSet { get; set; }

    [JsonProperty("hash")]
    public string Hash { get; set; } = "";

    [JsonProperty("title")]
    public string Title { get; set; } = "";

    [JsonProperty("artist")]
    public string Artist { get; set; } = "";

    [JsonProperty("difficulty")]
    public string Difficulty { get; set; } = "";

    [JsonProperty("mode")]
    public int Mode { get; set; }

    [JsonProperty("rating")]
    public double Rating { get; set; }

    [JsonProperty("status")]
    public int Status { get; set; }

    [JsonIgnore]
    public long MapperId { get; set; }

    [JsonProperty("mapper")]
    public UserShort Mapper => UserHelper.Get(MapperId)?.ToShort() ?? new UserShort();
}
