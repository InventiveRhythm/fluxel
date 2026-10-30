using System.ComponentModel.DataAnnotations.Schema;
using fluxel.Models.Users;
using Microsoft.EntityFrameworkCore;

namespace fluxel.Models.Maps;

[PrimaryKey(nameof(MapID), nameof(UserID))]
[Table("map_user_state")]
public class MapUserState
{
    #region Columns

    [Column("map")]
    public long MapID { get; init; }

    [Column("user")]
    public long UserID { get; init; }

    [Column("play_count")]
    public long PlayCount { get; set; }

    [Column("rate_purifier")]
    public bool? RateIsPurifier { get; set; }

    [Column("rate_chart")]
    public float? RateChart { get; set; }

    [Column("rate_reading")]
    public float? RateReading { get; set; }

    [Column("rate_tracking")]
    public float? RateTracking { get; set; }

    [Column("rate_perception")]
    public float? RatePerception { get; set; }

    #endregion

    #region Relations

    [ForeignKey(nameof(MapID))]
    public Map? Map { get; init; }

    [ForeignKey(nameof(UserID))]
    public User? User { get; init; }

    #endregion
}
