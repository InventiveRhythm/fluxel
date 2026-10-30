using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using fluxel.Models.Users;
using Microsoft.EntityFrameworkCore;

namespace fluxel.Models.Clubs;

[Index(nameof(ClubID))]
[Table("club_invite")]
public class ClubInvite
{
    #region Columns

    [Key, Column("code"), StringLength(7)]
    public string InviteCode { get; set; } = "";

    [Column("club")]
    public long ClubID { get; set; }

    [Column("user")]
    public long UserID { get; set; }

    #endregion

    #region Relations

    [ForeignKey(nameof(ClubID))]
    public Club? Club { get; init; }

    [ForeignKey(nameof(UserID))]
    public User? User { get; init; }

    #endregion
}
