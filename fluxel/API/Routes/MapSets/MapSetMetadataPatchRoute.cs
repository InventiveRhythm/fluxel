using System.Net.Http;
using System.Threading.Tasks;
using fluxel.API.Components;
using fluxel.Constants;
using fluxel.Database.Extensions;
using fluxel.Database.Helpers;
using fluXis.Online.API.Models.Maps;
using Midori.API.Components.Interfaces;
using Midori.Networking;
using Newtonsoft.Json;

namespace fluxel.API.Routes.MapSets;

public class MapSetMetadataPatchRoute : IFluxelAPIRoute, INeedsAuthorization
{
    public string RoutePath => "/mapset/:id/metadata";
    public HttpMethod Method => HttpMethod.Patch;

    public async Task Handle(FluxelAPIInteraction interaction)
    {
        if (!interaction.User.IsModerator())
        {
            await interaction.ReplyMessage(HttpStatusCode.Forbidden, ResponseStrings.NoPermission);
            return;
        }

        if (!interaction.TryParseBody<Payload>(out var payload))
        {
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, ResponseStrings.InvalidBodyJson);
            return;
        }

        if (!interaction.TryGetLongParameter("id", out var id))
        {
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, ResponseStrings.InvalidParameter("id", "long"));
            return;
        }

        var set = MapSetHelper.Get(id);

        if (set is null)
        {
            await interaction.ReplyMessage(HttpStatusCode.NotFound, ResponseStrings.MapSetNotFound);
            return;
        }

        if (payload.Flags.HasValue)
            set.Flags = payload.Flags.Value;

        MapSetHelper.Update(set);
        await interaction.Reply(HttpStatusCode.OK);
    }

    private class Payload
    {
        [JsonProperty("flags")]
        public MapSetFlag? Flags { get; set; }
    }
}
