using System.ComponentModel.DataAnnotations.Schema;
using fluxel.Models.Maps;
using Microsoft.EntityFrameworkCore;

namespace fluxel.Models.Clubs;

[PrimaryKey(nameof(MapID), nameof(ClubID))]
[Table("club_score")]
public class ClubScore
{
    #region Columns

    [Column("map")]
    public long MapID { get; init; }

    [Column("club")]
    public long ClubID { get; set; }

    [Column("performance_rating")]
    public double PerformanceRating { get; set; }

    [Column("score")]
    public int NumericScore { get; set; }

    [Column("accuracy")]
    public float Accuracy { get; set; }

    #endregion

    #region Relations

    [ForeignKey(nameof(MapID))]
    public Map? Map { get; init; }

    [ForeignKey(nameof(ClubID))]
    public Club? Club { get; init; }

    #endregion
}
