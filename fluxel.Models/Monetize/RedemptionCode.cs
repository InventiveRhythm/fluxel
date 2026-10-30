using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Midori.Utils;

namespace fluxel.Models.Monetize;

[Index(nameof(Code), IsUnique = true)]
[Table("redemption_code")]
public class RedemptionCode
{
    [Key, Column("id"), StringLength(32)]
    public string ID { get; init; } = Guid.NewGuid().ToString("N");

    [Column("code"), StringLength(24)]
    public string Code { get; init; } = RandomizeUtils.GenerateRandomString(24, CharacterType.Uppercase);

    [Column("uses_max")]
    public int UsesMaximum { get; init; }

    [Column("uses_current")]
    public int UsesCurrent { get; set; }
}
