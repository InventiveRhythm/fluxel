using System;
using System.Linq;
using fluxel.Models;
using fluxel.Models.Monetize;
using fluxel.Models.Users;
using Midori.API.Attributes;
using Midori.API.Components;
using Midori.Networking;

namespace fluxel.Services.API.Controllers;

[Controller("/codes")]
public class CodesController
{
    private readonly DatabaseContext database;

    public CodesController(DatabaseContext database)
    {
        this.database = database;
    }

    [Authenticated]
    [HttpRoute("/redeem", APIMethod.Post)]
    public APIReturn<object> Redeem(User auth, [Source(ParameterSource.Form)] string code)
    {
        var rc = database.RedemptionCodes.FirstOrDefault(x => x.Code == code);
        if (rc is null) return Returns.NotFound("redemption code");

        if (rc.UsesCurrent >= rc.UsesMaximum)
            return Returns.Message(HttpStatusCode.BadRequest, "Maximum code uses exceeded.");

        switch (rc)
        {
            case DonatorRedemptionCode dono:
            {
                using (database.EditAndSave())
                {
                    var now = DateTime.UtcNow;
                    var time = auth.SupportEndTime ?? now;

                    if (time < now)
                        time = now;

                    time += dono.DonateTime;
                    auth.SupportEndTime = time;
                }

                break;
            }

            default:
                throw new InvalidOperationException("Unknown redemption code type.");
        }

        using (database.EditAndSave())
            rc.UsesCurrent++;

        return Returns.Okay();
    }
}
