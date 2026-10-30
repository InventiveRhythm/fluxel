using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using fluxel.Models.Maps;
using Microsoft.EntityFrameworkCore;

namespace fluxel.Models.Clubs;

[Index(nameof(ClubID))]
[Table("club_claim")]
public class ClubClaim
{
    #region Columns

    [Key, Column("map")]
    public long MapID { get; init; }

    [Column("club")]
    public long ClubID { get; set; }

    #endregion

    #region Relations

    [ForeignKey(nameof(MapID))]
    public Map? Map { get; init; }

    [ForeignKey(nameof(ClubID))]
    public Club? Club { get; init; }

    #endregion
}
