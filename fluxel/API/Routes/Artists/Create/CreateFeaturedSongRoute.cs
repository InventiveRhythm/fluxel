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

public class CreateFeaturedSongRoute : IFluxelAPIRoute, INeedsAuthorization
{
    public string RoutePath => "/artists/:artist/albums/:album/tracks";
    public HttpMethod Method => HttpMethod.Post;

    public IEnumerable<(string, string)> Validate(FluxelAPIInteraction interaction)
    {
        if (!interaction.TryGetStringParameter("artist", out _))
            yield return ("_param", ResponseStrings.InvalidParameter("artist", "string"));
        if (!interaction.TryGetStringParameter("album", out _))
            yield return ("_param", ResponseStrings.InvalidParameter("album", "string"));

        if (!interaction.TryParseBody<APIFeaturedTrack>(out var payload))
            yield return ("_form", ResponseStrings.InvalidBodyJson);

        if (payload != null)
        {
            interaction.AddCache("payload", payload);

            if (string.IsNullOrWhiteSpace(payload.ID))
                yield return ("id", ResponseStrings.FieldRequired);
            if (string.IsNullOrWhiteSpace(payload.Name))
                yield return ("name", ResponseStrings.FieldRequired);
            if (string.IsNullOrWhiteSpace(payload.Length))
                yield return ("length", ResponseStrings.FieldRequired);
            if (string.IsNullOrWhiteSpace(payload.BPM))
                yield return ("bpm", ResponseStrings.FieldRequired);
            if (string.IsNullOrWhiteSpace(payload.Genre))
                yield return ("genre", ResponseStrings.FieldRequired);
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
        var albumId = interaction.GetStringParameter("album")!;

        if (!interaction.TryGetCache<APIFeaturedTrack>("payload", out var payload))
            throw new CacheMissingException("payload");

        var artist = FeaturedArtistHelper.GetArtist(artistId.ToLowerInvariant());

        if (artist is null)
        {
            await interaction.ReplyMessage(HttpStatusCode.NotFound, "No artist found with the provided ID.");
            return;
        }

        var album = FeaturedArtistHelper.GetAlbum(artist.ID, albumId.ToLowerInvariant());

        if (album is null)
        {
            await interaction.ReplyMessage(HttpStatusCode.NotFound, "No album found with the provided ID.");
            return;
        }

        if (FeaturedArtistHelper.GetTrack(artistId, album.AlbumID, payload.ID!.ToLowerInvariant()) != null)
        {
            interaction.AddError("id", "Song ID is already used.");
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, "Song ID is already used.");
            return;
        }

        var song = new FeaturedArtistTrack(artist.ID, album.AlbumID, payload.ID)
        {
            Name = payload.Name!,
            Length = payload.Length!,
            BPM = payload.BPM!,
            Genre = payload.Genre!
        };

        FeaturedArtistHelper.Add(song);
        await interaction.Reply(HttpStatusCode.Created, song);
    }
}
*/
