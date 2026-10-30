using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using fluXis.Online.API.Models.Featured;
using Microsoft.EntityFrameworkCore;

namespace fluxel.Models.Featured;

[PrimaryKey(nameof(ArtistID), nameof(AlbumID), nameof(ID))]
[Table("featured_track")]
public class FeaturedTrack
{
    #region Columns

    [Column("artist"), MaxLength(64)]
    public string ArtistID { get; set; } = string.Empty;

    [Column("album"), MaxLength(64)]
    public string AlbumID { get; set; } = string.Empty;

    [Column("id"), MaxLength(64)]
    public string ID { get; set; } = string.Empty;

    [Column("name"), MaxLength(256), Required]
    public string Name { get; set; } = string.Empty;

    [Column("length"), MaxLength(12)]
    public string? Length { get; set; }

    [Column("bpm"), MaxLength(16)]
    public string? BPM { get; set; }

    [Column("genre"), MaxLength(24)]
    public string? Genre { get; set; }

    #endregion

    #region Relations

    [ForeignKey(nameof(ArtistID))]
    public FeaturedArtist? Artist { get; init; }

    [ForeignKey($"{nameof(ArtistID)},{nameof(AlbumID)}")]
    public FeaturedAlbum? Album { get; init; }

    #endregion

    public APIFeaturedTrack ToAPI() => new()
    {
        ID = ID,
        Name = Name,
        Length = Length,
        BPM = BPM,
        Genre = Genre
    };
}
