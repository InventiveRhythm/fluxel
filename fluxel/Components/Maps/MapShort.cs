using fluxel.Components.Users;
using Newtonsoft.Json;

namespace fluxel.Components.Maps; 

public class MapShort {
    [JsonProperty("id")]
    public int Id { get; set; }
    
    [JsonProperty("mapset")]
    public int MapSet { get; set; }
    
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
    public int MapperId { get; set; }
    
    [JsonProperty("mapper")]
    public UserShort Mapper => User.FindById(MapperId)?.ToShort() ?? new UserShort();
}