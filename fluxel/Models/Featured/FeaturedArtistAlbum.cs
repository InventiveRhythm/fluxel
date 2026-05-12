using System;
using System.Linq;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace fluxel.Models.Featured;

[JsonObject(MemberSerialization.OptIn)]
public class FeaturedArtistAlbum
{
    [BsonId]
    public string InternalID { get; set; } = null!;

    [BsonIgnore]
    [JsonProperty("id")]
    public string AlbumID => InternalID.Split("/").Last();

    [BsonElement("name")]
    [JsonProperty("name")]
    public string Name { get; set; } = null!;

    [BsonElement("release")]
    [JsonProperty("release")]
    public AlbumRelease ReleaseDate { get; set; } = new();

    [BsonElement("colors")]
    [JsonProperty("colors")]
    public AlbumColors Colors { get; set; } = new();

    /*[BsonIgnore]
    [JsonProperty("tracks")]
    public List<FeaturedArtistTrack> Songs
    {
        get
        {
            var split = InternalID.Split("/");
            return FeaturedArtistHelper.FromAlbum(split[0], split[1]);
        }
    }*/

    public FeaturedArtistAlbum(string artist, string id)
    {
        InternalID = $"{artist}/{id}".ToLower();
    }

    [BsonConstructor]
    [Obsolete("BSON parsing")]
    public FeaturedArtistAlbum()
    {
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class AlbumRelease
    {
        [BsonElement("year")]
        [JsonProperty("year")]
        public int Year { get; set; }

        [BsonElement("month")]
        [JsonProperty("month")]
        public int Month { get; set; }

        [BsonElement("day")]
        [JsonProperty("day")]
        public int Day { get; set; }

        public int CompareTo(AlbumRelease other)
        {
            if (Year != other.Year)
                return Year.CompareTo(other.Year);

            if (Month != other.Month)
                return Month.CompareTo(other.Month);

            return Day.CompareTo(other.Day);
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class AlbumColors
    {
        [BsonElement("accent")]
        [JsonProperty("accent")]
        public string Accent { get; set; } = null!;

        [BsonElement("text-1")]
        [JsonProperty("text")]
        public string TextPrimary { get; set; } = null!;

        [BsonElement("text-2")]
        [JsonProperty("text2")]
        public string TextSecondary { get; set; } = null!;

        [BsonElement("bg-1")]
        [JsonProperty("bg")]
        public string BackgroundPrimary { get; set; } = null!;

        [BsonElement("bg-2")]
        [JsonProperty("bg2")]
        public string BackgroundSecondary { get; set; } = null!;
    }
}
