using System.Net.Http;
using System.Threading.Tasks;
using fluxel.API.Components;
using Midori.Networking;

namespace fluxel.API;

public class ServerErrorRoute : IFluxelAPIRoute
{
    public string RoutePath => "/500";
    public HttpMethod Method => HttpMethod.Get;

    public async Task Handle(FluxelAPIInteraction interaction)
        => await interaction.ReplyMessage(HttpStatusCode.InternalServerError, "Welp, something went very wrong. It's probably not your fault, but please report this to the developers.");
}
