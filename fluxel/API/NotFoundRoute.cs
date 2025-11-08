using System.Net.Http;
using System.Threading.Tasks;
using fluxel.API.Components;
using Midori.Networking;

namespace fluxel.API;

public class NotFoundRoute : IFluxelAPIRoute
{
    public string RoutePath => "/404";
    public HttpMethod Method => HttpMethod.Get;

    public async Task Handle(FluxelAPIInteraction interaction)
        => await interaction.ReplyMessage(HttpStatusCode.NotFound, "The requested route does not exist.");
}
