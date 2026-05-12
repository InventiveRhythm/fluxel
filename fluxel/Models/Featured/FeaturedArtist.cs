using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace fluxel.Models.Featured;

[JsonObject(MemberSerialization.OptIn)]
public class FeaturedArtist
{
    [BsonId]
    [JsonProperty("id")]
    public string ID { get; set; } = null!;

    [BsonElement("name")]
    [JsonProperty("name")]
    public string Name { get; set; } = null!;

    [BsonElement("description")]
    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("youtube")]
    [JsonProperty("youtube")]
    public string YouTube { get; set; } = string.Empty;

    [BsonElement("spotify")]
    [JsonProperty("spotify")]
    public string Spotify { get; set; } = string.Empty;

    [BsonElement("soundcloud")]
    [JsonProperty("soundcloud")]
    public string SoundCloud { get; set; } = string.Empty;

    [BsonElement("twitter")]
    [JsonProperty("twitter")]
    public string Twitter { get; set; } = string.Empty;

    [BsonElement("fluxis")]
    [JsonProperty("fluxis")]
    public string FluXis { get; set; } = string.Empty;

    [BsonElement("unofficial")]
    [JsonProperty("unofficial")]
    public bool Unofficial { get; set; }

    /*[BsonIgnore]
    [JsonProperty("albums")]
    public List<FeaturedArtistAlbum> Albums => FeaturedArtistHelper.FromArtist(ID);*/
}
