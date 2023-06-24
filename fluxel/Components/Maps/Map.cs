using fluxel.Components.Users;
using fluxel.Database;
using Newtonsoft.Json;
using Realms;

namespace fluxel.Components.Maps; 

public class Map : RealmObject {
    [PrimaryKey]
    [JsonProperty("id")]
    public int Id { get; set; }
    
    [JsonProperty("mapset")]
    public int SetId { get; set; }
    
    [JsonProperty("hash")]
    public string Hash { get; set; } = "";
    
    [JsonIgnore]
    public int MapperId { get; set; }
    
    [Ignored]
    [JsonProperty("mapper")]
    public UserShort Mapper => User.FindById(MapperId)?.ToShort() ?? new UserShort();
    
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

    public static Map FindById(int id) {
        return RealmAccess.Run(realm => realm.Find<Map>(id));
    }
}