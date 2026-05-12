/*
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using fluxel.API.Components;
using fluxel.Constants;
using fluXis.Online.API.Models.Featured;
using Midori.API.Components.Interfaces;
using Midori.Networking;

namespace fluxel.API.Routes.Artists.Patch;

public class PatchFeaturedAlbumRoute : IFluxelAPIRoute, INeedsAuthorization
{
    public string RoutePath => "/artists/:artist/albums/:album";
    public HttpMethod Method => HttpMethod.Patch;

    public IEnumerable<(string, string)> Validate(FluxelAPIInteraction interaction)
    {
        if (!interaction.TryGetStringParameter("artist", out _))
            yield return ("_param", ResponseStrings.InvalidParameter("artist", "string"));
        if (!interaction.TryGetStringParameter("album", out _))
            yield return ("_param", ResponseStrings.InvalidParameter("album", "string"));

        if (!interaction.TryParseBody<APIFeaturedAlbum>(out var payload))
            yield return ("_form", ResponseStrings.InvalidBodyJson);

        if (payload != null)
            interaction.AddCache("payload", payload);
    }

    public async Task Handle(FluxelAPIInteraction interaction)
    {
        if (!interaction.User.IsDeveloper())
        {
            await interaction.ReplyMessage(HttpStatusCode.Forbidden, "no");
            return;
        }

        if (!interaction.TryGetCache<APIFeaturedAlbum>("payload", out var payload))
            throw new CacheMissingException("payload");

        if (!FeaturedArtistHelper.TryGetArtist(interaction.GetStringParameter("artist")!.ToLower(), out var artist))
        {
            await interaction.ReplyMessage(HttpStatusCode.NotFound, "No artist found with the provided ID.");
            return;
        }

        if (!FeaturedArtistHelper.TryGetAlbum(artist.ID, interaction.GetStringParameter("album")!.ToLower(), out var album))
        {
            await interaction.ReplyMessage(HttpStatusCode.NotFound, "No album found with the provided ID.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(payload.Name))
            album.Name = payload.Name;

        if (payload.Release is not null)
        {
            album.ReleaseDate.Year = payload.Release.Year;
            album.ReleaseDate.Month = payload.Release.Month;
            album.ReleaseDate.Day = payload.Release.Day;
        }

        if (payload.Colors is not null)
        {
            if (!string.IsNullOrWhiteSpace(payload.Colors.Accent))
                album.Colors.Accent = payload.Colors.Accent;
            if (!string.IsNullOrWhiteSpace(payload.Colors.Text))
                album.Colors.TextPrimary = payload.Colors.Text;
            if (!string.IsNullOrWhiteSpace(payload.Colors.Text2))
                album.Colors.TextSecondary = payload.Colors.Text2;
            if (!string.IsNullOrWhiteSpace(payload.Colors.Background))
                album.Colors.BackgroundPrimary = payload.Colors.Background;
            if (!string.IsNullOrWhiteSpace(payload.Colors.Background2))
                album.Colors.BackgroundSecondary = payload.Colors.Background2;
        }

        FeaturedArtistHelper.UpdateAlbum(album);
        await interaction.Reply(HttpStatusCode.OK, album);
    }
}
*/
