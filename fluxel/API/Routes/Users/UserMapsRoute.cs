using System.Net.Http;
using System.Threading.Tasks;
using fluxel.API.Components;
using fluxel.Constants;
using fluxel.Database.Helpers;
using fluxel.Models.Users;
using Midori.Networking;

namespace fluxel.API.Routes.Users;

public class UserMapsRoute : IFluxelAPIRoute
{
    public string RoutePath => "/user/:id/maps";
    public HttpMethod Method => HttpMethod.Get;

    public async Task Handle(FluxelAPIInteraction interaction)
    {
        if (!interaction.TryGetLongParameter("id", out var id))
        {
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, ResponseStrings.InvalidParameter("id", "long"));
            return;
        }

        if (!UserHelper.TryGet(id, out var user))
        {
            await interaction.ReplyMessage(HttpStatusCode.NotFound, ResponseStrings.UserNotFound);
            return;
        }

        user.Cache = interaction.Cache;
        await interaction.Reply(HttpStatusCode.OK, new User.UserMaps(user, interaction.User));
    }
}
