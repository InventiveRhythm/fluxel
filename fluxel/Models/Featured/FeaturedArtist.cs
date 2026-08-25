using System.Linq;
using fluxel.Database;
using fluXis.Online.API.Models.Featured;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace fluxel.Models.Featured;

[JsonObject(MemberSerialization.OptIn)]
public class FeaturedArtist
{
    [BsonId]
    public string ID { get; set; } = null!;

    [BsonElement("name")]
    public string Name { get; set; } = null!;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("youtube")]
    public string YouTube { get; set; } = string.Empty;

    [BsonElement("spotify")]
    public string Spotify { get; set; } = string.Empty;

    [BsonElement("soundcloud")]
    public string SoundCloud { get; set; } = string.Empty;

    [BsonElement("twitter")]
    public string Twitter { get; set; } = string.Empty;

    [BsonElement("fluxis")]
    public string FluXis { get; set; } = string.Empty;

    [BsonElement("unofficial")]
    public bool Unofficial { get; set; }

    public APIFeaturedArtist ToAPI(ArtistManager artists) => new()
    {
        ID = ID,
        Name = Name,
        Description = Description,
        YouTube = YouTube,
        Spotify = Spotify,
        SoundCloud = SoundCloud,
        Twitter = Twitter,
        FluXis = FluXis,
        Albums = artists.FromArtist(ID).Select(x => x.ToAPI(artists)).ToList()
    };
}
