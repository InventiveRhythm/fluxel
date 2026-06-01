using System;
using System.Linq;
using fluXis.Online.API.Models.Featured;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace fluxel.Models.Featured;

[JsonObject(MemberSerialization.OptIn)]
public class FeaturedArtistTrack
{
    [BsonId]
    public string InternalID { get; set; } = null!;

    [BsonElement("name")]
    public string Name { get; set; } = null!;

    [BsonElement("length")]
    public string Length { get; set; } = null!;

    [BsonElement("bpm")]
    public string BPM { get; set; } = null!;

    [BsonElement("genre")]
    public string Genre { get; set; } = null!;

    public FeaturedArtistTrack(string artist, string album, string id)
    {
        InternalID = $"{artist}/{album}/{id}".ToLower();
    }

    [BsonConstructor]
    [Obsolete("BSON parsing")]
    public FeaturedArtistTrack()
    {
    }

    public APIFeaturedTrack ToAPI() => new()
    {
        ID = InternalID.Split("/").Last(),
        Name = Name,
        Length = Length,
        BPM = BPM,
        Genre = Genre
    };
}
