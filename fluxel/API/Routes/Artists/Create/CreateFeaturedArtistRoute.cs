/*
using System.Net.Http;
using System.Threading.Tasks;
using fluxel.Constants;
using fluxel.Models.Featured;
using fluxel.Utils;
using fluXis.Online.API.Models.Featured;
using Midori.API.Components.Interfaces;
using Midori.Networking;

namespace fluxel.API.Routes.Artists.Create;

public class CreateFeaturedArtistRoute : IFluxelAPIRoute, INeedsAuthorization
{
    public string RoutePath => "/artists";
    public HttpMethod Method => HttpMethod.Post;

    public async Task Handle(FluxelAPIInteraction interaction)
    {
        if (!interaction.User.IsDeveloper())
        {
            await interaction.ReplyMessage(HttpStatusCode.Forbidden, "no");
            return;
        }

        if (!interaction.TryParseBody<APIFeaturedArtist>(out var payload))
        {
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, ResponseStrings.InvalidBodyJson);
            return;
        }

        if (string.IsNullOrWhiteSpace(payload.ID))
        {
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, ResponseStrings.MissingJsonField<APIFeaturedArtist>(nameof(APIFeaturedArtist.ID)));
            return;
        }

        if (string.IsNullOrWhiteSpace(payload.Name))
        {
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, ResponseStrings.MissingJsonField<APIFeaturedArtist>(nameof(APIFeaturedArtist.Name)));
            return;
        }

        if (FeaturedArtistHelper.GetArtist(payload.ID.ToLowerInvariant()) is not null)
        {
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, "An artist with this ID already exists.");
            return;
        }

        if (!StringValidator.ValidateArtistID(payload.ID))
        {
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, "Invalid ID. Must be all lowercase and can only contain letters, numbers and dashes.");
            return;
        }

        var artist = new FeaturedArtist
        {
            ID = payload.ID,
            Name = payload.Name
        };

        FeaturedArtistHelper.Add(artist);
        await interaction.Reply(HttpStatusCode.OK, artist);
    }
}
*/
