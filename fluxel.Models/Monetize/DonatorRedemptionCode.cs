using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace fluxel.Models.Monetize;

public class DonatorRedemptionCode : RedemptionCode
{
    [Column("donate_time")]
    public TimeSpan DonateTime { get; init; }
}
