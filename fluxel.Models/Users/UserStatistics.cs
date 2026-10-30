using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using rhym;

namespace fluxel.Models.Users;

[PrimaryKey(nameof(UserID), nameof(Mode))]
[Table("user_statistics")]
public class UserStatistics
{
    #region Columns

    [Column("id")]
    public long UserID { get; init; }

    [Column("mode"), MaxLength(32), Required]
    public ResourceLocation Mode { get; init; }

    [Column("score")]
    public long TotalScore { get; set; }

    [Column("rating_overall")]
    public double RatingOverall { get; set; }

    [Column("rating_potential")]
    public double RatingPotential { get; set; }

    #endregion

    #region Relations

    [ForeignKey(nameof(UserID))]
    public User? User { get; init; }

    #endregion
}
