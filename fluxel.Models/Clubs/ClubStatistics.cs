using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using rhym;

namespace fluxel.Models.Clubs;

[PrimaryKey(nameof(ClubID), nameof(Mode))]
[Table("club_stats")]
public class ClubStatistics
{
    #region Columns

    [Column("id")]
    public long ClubID { get; init; }

    [Column("mode"), MaxLength(32), Required]
    public ResourceLocation Mode { get; init; }

    [Column("score")]
    public long TotalScore { get; set; }

    [Column("rating_overall")]
    public double RatingOverall { get; set; }

    #endregion

    #region Relations

    [ForeignKey(nameof(ClubID))]
    public Club? Club { get; init; }

    #endregion

    public ClubStatistics(long club, ResourceLocation mode)
    {
        ClubID = club;
        Mode = mode;
    }

    [UsedImplicitly]
    private ClubStatistics()
    {
    }
}
