using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using fluXis.Online.API.Models.Featured;
using Microsoft.EntityFrameworkCore;
using osu.Framework.Extensions.Color4Extensions;
using Color4 = osuTK.Graphics.Color4;

namespace fluxel.Models.Featured;

[PrimaryKey(nameof(ArtistID), nameof(ID))]
[Table("featured_album")]
public class FeaturedAlbum
{
    #region Columns

    [Column("artist"), MaxLength(64)]
    public string ArtistID { get; set; } = string.Empty;

    [Column("id"), MaxLength(64)]
    public string ID { get; set; } = string.Empty;

    [Column("name"), MaxLength(256), Required]
    public string Name { get; set; } = null!;

    [Column("release_year")]
    public int ReleaseYear { get; set; }

    [Column("release_month")]
    public int ReleaseMonth { get; set; }

    [Column("release_day")]
    public int ReleaseDay { get; set; }

    [Column("color_accent")]
    public Color4 ColorAccent { get; set; }

    [Column("color_text_1")]
    public Color4 ColorText1 { get; set; }

    [Column("color_text_2")]
    public Color4 ColorText2 { get; set; }

    [Column("color_background_1")]
    public Color4 ColorBackground1 { get; set; }

    [Column("color_background_2")]
    public Color4 ColorBackground2 { get; set; }

    #endregion

    #region Relations

    [ForeignKey(nameof(ArtistID))]
    public FeaturedArtist? Artist { get; init; }

    [InverseProperty(nameof(FeaturedTrack.Album))]
    public ICollection<FeaturedTrack> Tracks { get; init; } = [];

    #endregion

    public APIFeaturedAlbum ToAPI(DatabaseContext database)
    {
        database.Entry(this).Collection(x => x.Tracks).Load();

        return new APIFeaturedAlbum
        {
            ID = ID,
            Name = Name,
            Release = new APIFeaturedAlbum.AlbumRelease
            {
                Day = ReleaseDay,
                Month = ReleaseMonth,
                Year = ReleaseYear
            },
            Colors = new APIFeaturedAlbum.AlbumColors
            {
                Accent = ColorAccent.ToHex(),
                Text = ColorText1.ToHex(),
                Text2 = ColorText2.ToHex(),
                Background = ColorBackground1.ToHex(),
                Background2 = ColorBackground2.ToHex()
            },
            Tracks = [.. Tracks.Select(x => x.ToAPI())]
        };
    }
}
