using System.ComponentModel.DataAnnotations.Schema;
using fluxel.Models.Users;
using Microsoft.EntityFrameworkCore;

namespace fluxel.Models.Maps;

[PrimaryKey(nameof(MapSetID), nameof(UserID))]
[Table("mapset_user_state")]
public class MapSetUserState
{
    #region Columns

    [Column("mapset")]
    public long MapSetID { get; init; }

    [Column("user")]
    public long UserID { get; init; }

    [Column("favorite")]
    public bool Favorite { get; set; }

    [Column("vote")]
    public bool? Vote { get; set; }

    #endregion

    #region Relations

    [ForeignKey(nameof(MapSetID))]
    public MapSet? MapSet { get; init; }

    [ForeignKey(nameof(UserID))]
    public User? User { get; init; }

    #endregion
}
