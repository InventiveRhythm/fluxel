using System;
using System.Linq;
using System.Threading.Tasks;
using fluxel.Database;
using fluxel.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace fluxel.Tasks.Maps;

public class RefreshMapColorTask : IBasicTask
{
    public string Name => $"RefreshMapColor({id})";

    private readonly long id;

    public RefreshMapColorTask(long id)
    {
        this.id = id;
    }

    public Task Run(IServiceProvider services)
    {
        var mm = services.GetRequiredService<MapManager>();

        var map = mm.GetMap(id);
        if (map == null) throw new ArgumentException($"No map with ID {id} was found!");

        if (!ServerMapUtils.TryLoadFromZip(Assets.GetPathForAsset(AssetType.Map, map.SetID.ToString()), out var jsons))
            throw new Exception($"Failed to load mapset {map.ID}!");

        var json = jsons.FirstOrDefault(x => x.FileName.Equals(map.FileName, StringComparison.InvariantCultureIgnoreCase));
        if (json is null) throw new Exception($"Map file {map.FileName} does not exist in mapset {map.SetID}.");

        mm.QuickUpdate(id, m => m.Color = json.Colors.Accent.ToRGBA());
        return Task.CompletedTask;
    }
}
