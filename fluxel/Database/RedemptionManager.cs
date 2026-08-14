using System;
using System.Linq;
using fluxel.Models.Payment;
using Midori.Database;
using Midori.Utils;
using MongoDB.Bson;

namespace fluxel.Database;

/// <summary>
/// Manages redemption codes for donating and more.
/// </summary>
public class RedemptionManager
{
    private readonly IDatabaseTable<RedemptionCode> codes;

    public RedemptionManager(IDatabaseProvider db)
    {
        codes = db.GetTable<RedemptionCode>("redemption-codes");
    }

    public RedemptionCode Create(BsonDocument meta, int uses = 1, string? code = null)
    {
        var rc = new RedemptionCode(meta, uses, code);

        // check if code already exists
        while (FindByCode(rc.Code) != null)
        {
            if (code != null)
                throw new InvalidOperationException("This code has already been used.");

            rc.Code = RandomizeUtils.GenerateRandomString(24, CharacterType.Uppercase);
        }

        codes.Add(rc);
        return rc;
    }

    public RedemptionCode? FindByCode(string code)
    {
        code = code.ToUpperInvariant();
        return codes.Find(x => x.Code == code).FirstOrDefault();
    }

    public void Use(RedemptionCode code)
    {
        code.UsesCurrent++;
        codes.Replace(r => r.ID == code.ID, code);
    }
}
