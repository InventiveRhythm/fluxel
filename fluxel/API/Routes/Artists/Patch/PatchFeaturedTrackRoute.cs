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

public class PatchFeaturedTrackRoute : IFluxelAPIRoute, INeedsAuthorization
{
    public string RoutePath => "/artists/:artist/albums/:album/tracks/:track";
    public HttpMethod Method => HttpMethod.Patch;

    public IEnumerable<(string, string)> Validate(FluxelAPIInteraction interaction)
    {
        if (!interaction.TryGetStringParameter("artist", out _))
            yield return ("_param", ResponseStrings.InvalidParameter("artist", "string"));
        if (!interaction.TryGetStringParameter("album", out _))
            yield return ("_param", ResponseStrings.InvalidParameter("album", "string"));
        if (!interaction.TryGetStringParameter("track", out _))
            yield return ("_param", ResponseStrings.InvalidParameter("track", "string"));

        if (!interaction.TryParseBody<APIFeaturedTrack>(out var payload))
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

        if (!interaction.TryGetCache<APIFeaturedTrack>("payload", out var payload))
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

        if (!FeaturedArtistHelper.TryGetTrack(artist.ID, album.AlbumID, interaction.GetStringParameter("track")!.ToLower(), out var track))
        {
            await interaction.ReplyMessage(HttpStatusCode.NotFound, "No track found with the provided ID.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(payload.Name))
            track.Name = payload.Name;
        if (!string.IsNullOrWhiteSpace(payload.Length))
            track.Length = payload.Length;
        if (!string.IsNullOrWhiteSpace(payload.BPM))
            track.BPM = payload.BPM;
        if (!string.IsNullOrWhiteSpace(payload.Genre))
            track.Genre = payload.Genre;

        FeaturedArtistHelper.UpdateTrack(track);
        await interaction.Reply(HttpStatusCode.OK, track);
    }
}
*/
