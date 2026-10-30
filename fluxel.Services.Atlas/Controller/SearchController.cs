using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using fluxel.Components;
using fluxel.Models;
using fluxel.Models.Maps;
using fluxel.Models.Users;
using fluxel.Services.Atlas.Indexing;
using fluXis.Online.API.Models.Maps;
using Meilisearch;
using Midori.API.Attributes;
using Midori.API.Components;
using osu.Framework.Extensions.IEnumerableExtensions;

namespace fluxel.Services.Atlas.Controller;

[Controller("/search")]
public class SearchController
{
    private readonly Meili meili;
    private readonly DatabaseContext database;
    private readonly ModelTranslator translator;

    public SearchController(Meili meili, DatabaseContext database, ModelTranslator translator)
    {
        this.meili = meili;
        this.database = database;
        this.translator = translator;
    }

    [Authenticated(Required = false)]
    [HttpRoute("/mapsets")]
    public APIReturn<APIMapSet[]> MapSets(
        User? auth,
        [Source(ParameterSource.Query)] string? query,
        [Source(ParameterSource.Query)] int? offset,
        [Source(ParameterSource.Query)] string? sort,
        [Source(ParameterSource.Query)] string? status,
        [Source(ParameterSource.Query)] string? modes
    )
    {
        if (string.IsNullOrWhiteSpace(status))
            status = "pure,ranked";

        List<string> filters =
        [
            string.Join(" OR ", status.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                      .Select(x => $"status = '{x}'"))
        ];

        if (!string.IsNullOrWhiteSpace(modes))
        {
            var spl = modes.Split(',');
            spl.ForEach(x => filters.Add($"modes = '{x}'"));
        }

        var search = new SearchQuery
        {
            Filter = filters.ToArray(),
            Offset = offset,
            Limit = 32
        };

        switch (sort)
        {
            case null:
                search.Sort = ["ranked:desc", "created:desc"];
                break;

            default:
                search.Sort = [.. sort.Split(',')];
                break;
        }

        var results = meili.MapSets.SearchAsync<IndexedMapSet>(query, search).Result;
        var sets = results.Hits.Select(x => database.MapSets.Find(x.ID)).ToImmutableArray();
        return sets.OfType<MapSet>().Select(x => translator.ToAPI(x, userid: auth?.ID)).ToArray();
    }

    // TODO: move to admin and create a valkey event
    /*[Authenticated(Scopes.DEV)]*/
    [HttpRoute("/mapsets/reindex", APIMethod.Post)]
    public APIReturn<object> ReindexMapSets()
    {
        var info = meili.ReIndexAllMapSets(database).Result;
        return info;
    }
}
