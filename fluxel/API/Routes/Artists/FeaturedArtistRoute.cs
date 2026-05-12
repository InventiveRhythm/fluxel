/*
using System.Net.Http;
using System.Threading.Tasks;
using fluxel.Constants;
using Midori.Networking;

namespace fluxel.API.Routes.Artists;

public class FeaturedArtistRoute : IFluxelAPIRoute
{
    public string RoutePath => "/artists/:id";
    public HttpMethod Method => HttpMethod.Get;

    public async Task Handle(FluxelAPIInteraction interaction)
    {
        if (!interaction.TryGetStringParameter("id", out var id))
        {
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, ResponseStrings.InvalidParameter("id", "string"));
            return;
        }

        var artist = FeaturedArtistHelper.GetArtist(id);

        if (artist is null)
        {
            await interaction.Reply(HttpStatusCode.NotFound, ResponseStrings.ProvidedIDNotFound("artist"));
            return;
        }

        await interaction.Reply(HttpStatusCode.OK, artist);
    }
}
*/
