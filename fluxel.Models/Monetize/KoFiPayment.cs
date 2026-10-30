using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace fluxel.Models.Monetize;

[Table("kofi_payment")]
public class KoFiPayment
{
    [Key, Column("id"), MaxLength(64)]
    public string MessageID { get; init; } = string.Empty;

    [Column("raw"), MaxLength(4096)]
    public string RawEvent { get; init; } = string.Empty;

    [Column("email"), MaxLength(128)]
    public string Email { get; init; } = string.Empty;

    [Column("amount")]
    public double Amount { get; init; }

    [Column("redeem")]
    public string? RedemptionCodeID { get; init; }

    [ForeignKey(nameof(RedemptionCodeID))]
    public RedemptionCode? RedemptionCode { get; set; }
}
