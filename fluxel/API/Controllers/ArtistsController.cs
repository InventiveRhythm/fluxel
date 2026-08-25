using System.Collections.Generic;
using System.Linq;
using fluxel.Database;
using fluXis.Online.API.Models.Featured;
using Midori.API.Attributes;
using Midori.API.Components;

namespace fluxel.API.Controllers;

[Controller("/artists")]
public class ArtistsController
{
    private readonly ArtistManager artists;

    public ArtistsController(ArtistManager artists)
    {
        this.artists = artists;
    }

    [HttpRoute("/")]
    public APIReturn<List<APIFeaturedArtist>> List()
        => artists.AllArtists.Select(x => x.ToAPI(artists)).ToList();

    [HttpRoute("/:id")]
    public APIReturn<APIFeaturedArtist> ByID(string id)
    {
        var art = artists.GetArtist(id);
        if (art is null) return Returns.NotFound("artist");

        return art.ToAPI(artists);
    }
}
