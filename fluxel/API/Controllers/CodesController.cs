using System;
using fluxel.Database;
using fluxel.Models.Payment;
using fluxel.Models.Users;
using Midori.API.Attributes;
using Midori.API.Components;
using Midori.Networking;

namespace fluxel.API.Controllers;

[Controller("/codes")]
public class CodesController
{
    private readonly RedemptionManager redemptions;
    private readonly UserManager users;

    public CodesController(RedemptionManager redemptions, UserManager users)
    {
        this.redemptions = redemptions;
        this.users = users;
    }

    [Authenticated]
    [HttpRoute("/redeem", APIMethod.Post)]
    public APIReturn<object> Redeem(User auth, [Source(ParameterSource.Form)] string code)
    {
        var rc = redemptions.FindByCode(code);
        if (rc is null) return Returns.NotFound("redemption code");

        if (rc.UsesCurrent >= rc.UsesMax)
            return Returns.Message(HttpStatusCode.BadRequest, "Maximum code uses exceeded.");

        redemptions.Use(rc);

        if (rc.Metadata.Contains(RedemptionCode.DONATE_AMOUNT))
        {
            var amount = rc.Metadata[RedemptionCode.DONATE_AMOUNT].AsDouble;

            users.UpdateLocked(auth.ID, u =>
            {
                var now = DateTime.UtcNow;
                var time = u.SupportEndTime ?? now;

                if (time < now)
                    time = now;

                var days = amount / 2d * 31;
                time = time.AddDays(days);
                u.SupportEndTime = time;
            });
        }

        return Returns.Okay();
    }
}
