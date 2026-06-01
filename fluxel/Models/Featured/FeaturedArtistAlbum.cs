using System;
using System.Linq;
using fluxel.Database;
using fluXis.Online.API.Models.Featured;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace fluxel.Models.Featured;

[JsonObject(MemberSerialization.OptIn)]
public class FeaturedArtistAlbum
{
    [BsonId]
    public string InternalID { get; set; } = null!;

    [BsonElement("name")]
    public string Name { get; set; } = null!;

    [BsonElement("release")]
    public AlbumRelease ReleaseDate { get; set; } = new();

    [BsonElement("colors")]
    public AlbumColors Colors { get; set; } = new();

    public FeaturedArtistAlbum(string artist, string id)
    {
        InternalID = $"{artist}/{id}".ToLower();
    }

    [BsonConstructor]
    [Obsolete("BSON parsing")]
    public FeaturedArtistAlbum()
    {
    }

    public APIFeaturedAlbum ToAPI(ArtistManager artists)
    {
        var split = InternalID.Split("/");

        return new APIFeaturedAlbum
        {
            ID = InternalID.Split("/").Last(),
            Name = Name,
            Release = ReleaseDate.ToAPI(),
            Colors = Colors.ToAPI(),
            Tracks = artists.FromAlbum(split.First(), split.Last()).Select(x => x.ToAPI()).ToList()
        };
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class AlbumRelease
    {
        [BsonElement("year")]
        public int Year { get; set; }

        [BsonElement("month")]
        public int Month { get; set; }

        [BsonElement("day")]
        public int Day { get; set; }

        public int CompareTo(AlbumRelease other)
        {
            if (Year != other.Year)
                return Year.CompareTo(other.Year);

            if (Month != other.Month)
                return Month.CompareTo(other.Month);

            return Day.CompareTo(other.Day);
        }

        public APIFeaturedAlbum.AlbumRelease ToAPI() => new()
        {
            Year = Year,
            Month = Month,
            Day = Day
        };
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class AlbumColors
    {
        [BsonElement("accent")]
        public string Accent { get; set; } = null!;

        [BsonElement("text-1")]
        public string TextPrimary { get; set; } = null!;

        [BsonElement("text-2")]
        public string TextSecondary { get; set; } = null!;

        [BsonElement("bg-1")]
        public string BackgroundPrimary { get; set; } = null!;

        [BsonElement("bg-2")]
        public string BackgroundSecondary { get; set; } = null!;

        public APIFeaturedAlbum.AlbumColors ToAPI() => new()
        {
            Accent = Accent,
            Text = TextPrimary,
            Text2 = TextSecondary,
            Background = BackgroundPrimary,
            Background2 = BackgroundSecondary
        };
    }
}
