using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using fluxel.Models.Maps.Flags;
using fluxel.Models.Users;
using fluXis.Online.API.Models.Maps;

namespace fluxel.Models.Maps;

[Table("mapset")]
public class MapSet
{
    public const long MAX_MAPSETS_IN_QUEUE = 2;
    public const int MAX_PACKAGE_SIZE = 75 * 1024 * 1024;
    public const int REQUIRED_VOTES = 2;

    [Key, Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long ID { get; init; }

    [Column("user")]
    public long UserID { get; init; }

    [Column("title"), MaxLength(256)]
    public string Title { get; set; } = string.Empty;

    [Column("title_romanized"), MaxLength(256)]
    public string? TitleRomanized { get; set; }

    [Column("artist"), MaxLength(256)]
    public string Artist { get; set; } = string.Empty;

    [Column("artist_romanized"), MaxLength(256)]
    public string? ArtistRomanized { get; set; }

    [Column("status")]
    public MapSetStatus Status { get; set; }

    [Column("submitted")]
    public DateTime Submitted { get; set; } = DateTime.UtcNow;

    [Column("updated")]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    [Column("queue_submitted")]
    public DateTime? QueueSubmitted { get; set; }

    [Column("ranked")]
    public DateTime? DateRanked { get; set; }

    [Column("flags")]
    public MapSetFlag Flags { get; set; }

    [Column("internal-flags")]
    public MapSetFlagsInternal InternalFlags { get; set; }

    #region Relations

    [ForeignKey(nameof(UserID))]
    public User? User { get; init; }

    [InverseProperty(nameof(Map.MapSet))]
    public ICollection<Map> Maps { get; init; } = [];

    [InverseProperty(nameof(MapSetUserState.MapSet))]
    public ICollection<MapSetUserState> UserStates { get; init; } = [];

    #endregion
}
