using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fluxel.Components.IPC;
using fluxel.Config;
using fluxel.Models;
using fluxel.Models.Maps;
using fluxel.Services.Atlas.Indexing;
using Meilisearch;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using osu.Framework.Extensions.IEnumerableExtensions;
using Index = Meilisearch.Index;

namespace fluxel.Services.Atlas;

public class Meili : IDisposable
{
    public Index MapSets { get; }

    private readonly IServiceProvider services;

    private readonly IDisposable mapsetCreate;
    private readonly IDisposable mapsetUpdate;

    public Meili(ServerConfig config, Valkey valkey, IServiceProvider services)
    {
        this.services = services;
        var client = new MeilisearchClient(config.MeilisearchUrl, config.MeilisearchKey);
        MapSets = client.Index("mapsets");

        mapsetCreate = valkey.Subscribe<ValkeyIDEvent>(Valkey.MapSetCreate, e => _ = IndexMapSet(e.ID)).Result;
        mapsetUpdate = valkey.Subscribe<ValkeyIDEvent>(Valkey.MapSetUpdate, e => _ = ReIndexMapSet(e.ID)).Result;
    }

    public async Task IndexMapSet(long id)
    {
        using var svc = services.CreateScope();
        using var database = svc.ServiceProvider.GetRequiredService<DatabaseContext>();

        var set = database.MapSets.Include(x => x.Maps).FirstOrDefault(x => x.ID == id);
        if (set is null) return;

        await MapSets.AddDocumentsAsync(createIndexed(set).Yield());
    }

    public async Task ReIndexMapSet(long id)
    {
        using var svc = services.CreateScope();
        var database = svc.ServiceProvider.GetRequiredService<DatabaseContext>();

        var set = database.MapSets.Include(x => x.Maps).FirstOrDefault(x => x.ID == id);
        if (set is null) return;

        await MapSets.UpdateDocumentsAsync(createIndexed(set).Yield());
    }

    public async Task<TaskInfo> ReIndexAllMapSets(DatabaseContext database)
    {
        await MapSets.DeleteAllDocumentsAsync();
        await MapSets.UpdateSortableAttributesAsync(["ranked", "created"]);
        await MapSets.UpdateFilterableAttributesAsync(["status", "modes"]);

        var sets = database.MapSets.Include(x => x.Maps).ToArray();
        return await MapSets.AddDocumentsAsync(sets.Select(createIndexed));
    }

    private static IndexedMapSet createIndexed(MapSet set) => new()
    {
        ID = set.ID,
        Title = set.Title,
        Artist = set.Artist,
        TitleRomanized = set.TitleRomanized ?? string.Empty,
        ArtistRomanized = set.ArtistRomanized ?? string.Empty,
        Status = set.Status.ToString().ToLowerInvariant(),
        Modes = [.. set.Maps.Select(m => m.Mode.ToString()).Distinct()],
        Sources =
        [
            .. set.Maps.Select(m => m.Sources.GetValueOrDefault("audio", string.Empty))
                  .Where(s => !string.IsNullOrWhiteSpace(s))
                  .Distinct()
        ],
        Tags = [.. set.Maps.SelectMany(m => m.Tags).Distinct()],
        DateCreated = set.Submitted,
        DateRanked = set.DateRanked
    };

    public void Dispose()
    {
        mapsetCreate.Dispose();
        mapsetUpdate.Dispose();
    }
}
