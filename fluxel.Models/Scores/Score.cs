using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using fluxel.Models.Maps;
using fluxel.Models.Users;
using fluXis.Scoring.Enums;

namespace fluxel.Models.Scores;

[Table("score")]
public class Score
{
    #region Columns

    [Key, Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long ID { get; init; }

    [Column("map")]
    public long MapID { get; init; }

    [Column("user")]
    public long UserID { get; init; }

    [Column("map_hash"), StringLength(64)]
    public string MapHash { get; init; } = string.Empty;

    [Column("time")]
    public DateTime Time { get; init; } = DateTime.UtcNow;

    [Column("mods")]
    public string[] Mods { get; init; } = [];

    [Column("scroll_speed")]
    public float ScrollSpeed { get; init; }

    [Column("replay")]
    public bool HasReplay { get; init; }

    [Column("performance_rating")]
    public double PerformanceRating { get; set; }

    [Column("score")]
    public int NumericScore { get; set; }

    [Column("accuracy")]
    public float Accuracy { get; set; }

    [Column("rank")]
    public ScoreRank Rank { get; set; }

    [Column("highest_combo")]
    public int HighestCombo { get; set; }

    [Column("flawless")]
    public int FlawlessCount { get; set; }

    [Column("perfect")]
    public int PerfectCount { get; set; }

    [Column("great")]
    public int GreatCount { get; set; }

    [Column("alright")]
    public int AlrightCount { get; set; }

    [Column("okay")]
    public int OkayCount { get; set; }

    [Column("miss")]
    public int MissCount { get; set; }

    #endregion

    #region Relations

    [ForeignKey(nameof(UserID))]
    public User? User { get; init; }

    [ForeignKey(nameof(MapID))]
    public Map? Map { get; init; }

    #endregion
}
