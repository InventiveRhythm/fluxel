using System.Linq;
using fluxel.Models;
using fluXis.Online.API.Models.Featured;
using Midori.API.Attributes;
using Midori.API.Components;

namespace fluxel.Services.API.Controllers;

[Controller("/artists")]
public class ArtistsController
{
    private readonly DatabaseContext database;

    public ArtistsController(DatabaseContext database)
    {
        this.database = database;
    }

    [HttpRoute("/")]
    public APIReturn<APIFeaturedArtist[]> List()
        => database.FeaturedArtists.ToArray().Select(x => x.ToAPI(database)).ToArray();

    [HttpRoute("/:id")]
    public APIReturn<APIFeaturedArtist> ByID(string id)
    {
        var art = database.FeaturedArtists.Find(id);
        if (art is null) return Returns.NotFound("artist");

        return art.ToAPI(database);
    }
}
