using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using fluXis.Graphics.UserInterface.Color;
using Microsoft.EntityFrameworkCore;
using osuTK.Graphics;
using rhym;

namespace fluxel.Models.Maps;

[Index(nameof(SHA256Hash))]
[Table("map")]
public class Map
{
    #region Columns

    [Key, Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long ID { get; init; }

    [Column("mapset"), Required]
    public long MapSetID { get; init; }

    [Column("file"), MaxLength(256), Required]
    public string FileName { get; init; } = string.Empty;

    [Column("chart_hash"), StringLength(64), Required]
    public string SHA256Hash { get; set; } = string.Empty;

    [Column("event_hash"), StringLength(64)]
    public string? EventSHA256Hash { get; set; }

    [Column("storyboard_hash"), StringLength(64)]
    public string? StoryboardSHA256Hash { get; set; }

    [Column("mappers")]
    public long[] MapperIDs { get; set; } = [];

    [Column("difficulty_name"), MaxLength(512)]
    public string DifficultyName { get; set; } = string.Empty;

    [Column("difficulty_rating")]
    public double DifficultyRating { get; set; }

    [Column("difficulty_accuracy")]
    public float AccuracyDifficulty { get; set; }

    [Column("difficulty_health")]
    public float HealthDifficulty { get; set; }

    [Column("mode"), Required]
    public ResourceLocation Mode { get; set; }

    [Column("title"), MaxLength(256)]
    public string Title { get; set; } = string.Empty;

    [Column("title_romanized"), MaxLength(256)]
    public string? TitleRomanized { get; set; }

    [Column("artist"), MaxLength(256)]
    public string Artist { get; set; } = string.Empty;

    [Column("artist_romanized"), MaxLength(256)]
    public string? ArtistRomanized { get; set; }

    [Column("sources", TypeName = "jsonb")]
    public Dictionary<string, string> Sources { get; set; } = [];

    [Column("tags")]
    public string[] Tags { get; set; } = [];

    [Column("bpm_min")]
    public double? BPMMin { get; set; }

    [Column("bpm_max")]
    public double? BPMMax { get; set; }

    [Column("bpm")]
    public double BPM { get; set; }

    [Column("length")]
    public long Length { get; set; }

    // column name is defined in context
    public Dictionary<ResourceLocation, long> HitObjectCounts { get; set; } = [];

    // this is just a list of events used in the map
    // some special rules apply for things like SVs
    [Column("events")]
    public string[] EventTagsRaw { get; set; } = [];

    [Column("accent_color")]
    public Color4 AccentColor { get; set; } = Theme.Highlight;

    #endregion

    #region Relations

    [ForeignKey(nameof(MapSetID))]
    public MapSet? MapSet { get; init; }

    [InverseProperty(nameof(MapUserState.Map))]
    public ICollection<MapUserState> UserStates { get; init; } = [];

    #endregion

    [NotMapped]
    public ResourceLocation[] EventTags
    {
        get => [.. EventTagsRaw.Select(ResourceLocation.FromString)];
        set => EventTagsRaw = [.. value.Select(x => x.ToString())];
    }
}
