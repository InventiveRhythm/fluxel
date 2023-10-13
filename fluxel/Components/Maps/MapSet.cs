using fluxel.Components.Users;
using fluxel.Database.Helpers;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace fluxel.Components.Maps;

public class MapSet {
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonIgnore]
    public long CreatorId { get; init; }

    [BsonIgnore]
    [JsonProperty("creator")]
    public UserShort Creator => UserHelper.Get(CreatorId)?.ToShort() ?? new UserShort();

    [JsonProperty("artist")]
    public string Artist { get; set; } = "";

    [JsonProperty("title")]
    public string Title { get; set; } = "";

    [JsonProperty("status")]
    public int Status { get; set; }

    [JsonIgnore]
    public string Maps { get; set; } = "";

    [BsonIgnore]
    [JsonProperty("maps")]
    public List<Map> MapsList {
        get {
            var split = Maps.Split(',');
            var maps = new List<Map>();

            foreach (var s in split)
            {
                if (!int.TryParse(s, out var id)) continue;

                var map = MapHelper.Get(id);

                if (map is not null)
                    maps.Add(map);
            }

            return maps;
        }
    }

    [JsonIgnore]
    public DateTimeOffset Submitted { get; set; } = DateTimeOffset.UtcNow;

    [JsonIgnore]
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;

    [BsonIgnore]
    [JsonProperty("submitted")]
    public long SubmittedLong => Submitted.ToUnixTimeSeconds();

    [BsonIgnore]
    [JsonProperty("last_updated")]
    public long LastUpdatedLong => LastUpdated.ToUnixTimeSeconds();

    [BsonIgnore]
    [JsonProperty("tags")]
    public string[] Tags {
        get {
            var tags = new List<string>();

            MapsList.ForEach(map => {
                var split = map.Tags.Split(',');

                foreach (var s in split) {
                    if (!tags.Contains(s)) {
                        tags.Add(s.Trim());
                    }
                }
            });

            return tags.ToArray();
        }
    }

    [BsonIgnore]
    [JsonProperty("source")]
    public string Source {
        get {
            Dictionary<string, int> sources = new();

            MapsList.ForEach(map => {
                if (sources.ContainsKey(map.Source)) {
                    sources[map.Source]++;
                }
                else {
                    sources.Add(map.Source, 1);
                }
            });

            return sources.Count == 0 ? "" : sources.MaxBy(pair => pair.Value).Key;
        }
    }
}
