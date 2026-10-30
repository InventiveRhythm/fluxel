using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using fluXis.Online.API.Models.Featured;
using Microsoft.EntityFrameworkCore;

namespace fluxel.Models.Featured;

[PrimaryKey(nameof(ID))]
[Table("featured_artist")]
public class FeaturedArtist
{
    #region Columns

    [Column("id"), MaxLength(64)]
    public string ID { get; set; } = string.Empty;

    [Column("name"), MaxLength(64), Required]
    public string Name { get; set; } = string.Empty;

    [Column("description"), MaxLength(64)]
    public string? Description { get; set; }

    [Column("youtube"), MaxLength(64)]
    public string? YouTube { get; set; }

    [Column("spotify"), MaxLength(64)]
    public string? Spotify { get; set; }

    [Column("soundcloud"), MaxLength(64)]
    public string? SoundCloud { get; set; }

    [Column("twitter"), MaxLength(64)]
    public string? Twitter { get; set; }

    [Column("fluxis")]
    public long? FluXis { get; set; }

    [Column("unofficial")]
    public bool Unofficial { get; set; }

    #endregion

    #region Relations

    [InverseProperty(nameof(FeaturedAlbum.Artist))]
    public ICollection<FeaturedAlbum> Albums { get; init; } = [];

    #endregion

    public APIFeaturedArtist ToAPI(DatabaseContext database)
    {
        database.Entry(this).Collection(x => x.Albums).Load();

        return new APIFeaturedArtist
        {
            ID = ID,
            Name = Name,
            Description = Description,
            YouTube = YouTube,
            Spotify = Spotify,
            SoundCloud = SoundCloud,
            Twitter = Twitter,
            FluXis = FluXis,
            Albums = [.. Albums.Select(x => x.ToAPI(database))]
        };
    }
}
