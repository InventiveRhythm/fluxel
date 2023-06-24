using fluxel.Components.Users;
using fluxel.Database;
using Newtonsoft.Json;
using Realms;

namespace fluxel.Components.Maps; 

public class MapSet : RealmObject {
    [PrimaryKey]
    [JsonProperty("id")]
    public int Id { get; set; }
    
    [JsonIgnore]
    public int CreatorId { get; set; }

    [Ignored]
    [JsonProperty("creator")]
    public UserShort Creator => User.FindById(CreatorId)?.ToShort() ?? new UserShort();
    
    [JsonProperty("artist")]
    public string Artist { get; set; } = "";
    
    [JsonProperty("title")]
    public string Title { get; set; } = "";
    
    [JsonProperty("status")]
    public int Status { get; set; }
    
    [JsonIgnore]
    public string Maps { get; set; } = "";

    [Ignored]
    [JsonProperty("maps")]
    public List<Map> MapsList {
        get {
            var split = Maps.Split(',');
            var maps = new List<Map>();
            
            foreach (var s in split) {
                if (int.TryParse(s, out int id)) {
                    maps.Add(Map.FindById(id));
                }
            }
            
            return maps;
        }
    }

    [JsonIgnore]
    public DateTimeOffset Submitted { get; set; } = DateTimeOffset.UtcNow;
    
    [JsonIgnore]
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
    
    [Ignored]
    [JsonProperty("submitted")]
    public long SubmittedLong => Submitted.ToUnixTimeSeconds();
    
    [Ignored]
    [JsonProperty("last_updated")]
    public long LastUpdatedLong => LastUpdated.ToUnixTimeSeconds();

    [Ignored]
    [JsonProperty("tags")]
    public string[] Tags {
        get {
            var tags = new List<string>();
            
            MapsList.ForEach(map => {
                var split = map.Tags.Split(',');
                
                foreach (var s in split) {
                    if (!tags.Contains(s)) {
                        tags.Add(s);
                    }
                }
            });
            
            return tags.ToArray();
        }
    }
    
    [Ignored]
    [JsonProperty("source")]
    public string Source {
        get {
            Dictionary<string, int> sources = new();
            
            MapsList.ForEach(map => {
                if (sources.ContainsKey(map.Source)) {
                    sources[map.Source]++;
                } else {
                    sources.Add(map.Source, 1);
                }
            });
            
            return sources.Count == 0 ? "" : sources.MaxBy(pair => pair.Value).Key;
        }
    }

    public static int GetNextId() {
        return RealmAccess.Run(realm => {
            var users = realm.All<MapSet>();
            
            var max = 0;
            
            foreach (var user in users) {
                if (user.Id > max) {
                    max = user.Id;
                }
            }
            
            return !users.Any() ? 1 : max + 1;
        });
    }

    public static MapSet FindById(int id) {
        return RealmAccess.Run(realm => realm.Find<MapSet>(id));
    }
}