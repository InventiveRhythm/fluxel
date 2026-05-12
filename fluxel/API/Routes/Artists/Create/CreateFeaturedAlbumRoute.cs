/*
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using fluxel.API.Components;
using fluxel.Constants;
using fluxel.Models.Featured;
using fluXis.Online.API.Models.Featured;
using Midori.API.Components.Interfaces;
using Midori.Networking;

namespace fluxel.API.Routes.Artists.Create;

public class CreateFeaturedAlbumRoute : IFluxelAPIRoute, INeedsAuthorization
{
    public string RoutePath => "/artists/:artist/albums";
    public HttpMethod Method => HttpMethod.Post;

    public IEnumerable<(string, string)> Validate(FluxelAPIInteraction interaction)
    {
        if (!interaction.TryGetStringParameter("artist", out _))
            yield return ("_param", ResponseStrings.InvalidParameter("artist", "string"));

        if (!interaction.TryParseBody<APIFeaturedAlbum>(out var payload))
            yield return ("_form", ResponseStrings.InvalidBodyJson);

        if (payload != null)
        {
            interaction.AddCache("payload", payload);

            if (string.IsNullOrWhiteSpace(payload.ID))
                yield return ("id", ResponseStrings.BodyMissingProperty("id"));
            if (string.IsNullOrWhiteSpace(payload.Name))
                yield return ("name", ResponseStrings.BodyMissingProperty("name"));
            if (payload.Release is null)
                yield return ("release", ResponseStrings.BodyMissingProperty("release"));
        }
    }

    public async Task Handle(FluxelAPIInteraction interaction)
    {
        if (!interaction.User.IsDeveloper())
        {
            await interaction.ReplyMessage(HttpStatusCode.Forbidden, "no");
            return;
        }

        var artistId = interaction.GetStringParameter("artist")!;

        if (!interaction.TryGetCache<APIFeaturedAlbum>("payload", out var payload))
            throw new CacheMissingException("payload");

        var artist = FeaturedArtistHelper.GetArtist(artistId.ToLowerInvariant());

        if (artist is null)
        {
            await interaction.ReplyMessage(HttpStatusCode.NotFound, "No artist found with the provided ID.");
            return;
        }

        if (FeaturedArtistHelper.GetAlbum(artistId, payload.ID!.ToLowerInvariant()) != null)
        {
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, "Album ID is already used.");
            return;
        }

        payload.Colors ??= new APIFeaturedAlbum.AlbumColors();

        var album = new FeaturedArtistAlbum(artist.ID, payload.ID)
        {
            Name = payload.Name!,
            ReleaseDate = new FeaturedArtistAlbum.AlbumRelease
            {
                Year = payload.Release.Year,
                Month = payload.Release.Month,
                Day = payload.Release.Day
            },
            Colors = new FeaturedArtistAlbum.AlbumColors
            {
                Accent = payload.Colors.Accent,
                TextPrimary = payload.Colors.Text,
                TextSecondary = payload.Colors.Text2,
                BackgroundPrimary = payload.Colors.Background,
                BackgroundSecondary = payload.Colors.Background2
            }
        };

        FeaturedArtistHelper.Add(album);
        await interaction.Reply(HttpStatusCode.Created, album);
    }
}
*/
