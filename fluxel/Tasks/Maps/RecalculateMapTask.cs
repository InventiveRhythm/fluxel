using System;
using System.Linq;
using System.Threading.Tasks;
using fluxel.Database;
using fluxel.Utils;
using fluXis.Map.Structures;
using fluXis.Online.API.Models.Maps;
using fluXis.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace fluxel.Tasks.Maps;

public class RecalculateMapTask : IBasicTask
{
    public string Name => $"RecalculateMap({id})";

    private long id { get; }

    public RecalculateMapTask(long id)
    {
        this.id = id;
    }

    public Task Run(IServiceProvider services)
    {
        var maps = services.GetRequiredService<MapManager>();
        var dbMap = maps.GetMap(id);

        if (dbMap == null)
            throw new ArgumentException($"No map with id {id} was found!");

        var path = $"{Environment.CurrentDirectory}/Assets/map";

        if (!ServerMapUtils.OpenArchive($"{path}/{dbMap.SetID}.zip", out var archive))
            throw new Exception($"Failed to load mapset {dbMap.SetID}!");

        if (!archive.Maps.TryGetValue(dbMap.FileName, out var map))
            throw new Exception($"Map {dbMap.DifficultyName} ({dbMap.FileName}) not found in mapset {dbMap.SetID}!");

        if (!string.IsNullOrEmpty(map.EffectFile) && archive.Events.TryGetValue(map.EffectFile, out var effects))
        {
            dbMap.EffectSHA256Hash = MapUtils.GetHash(effects.RawContent);
            dbMap.Effects = MapUtils.GetEffects(effects);
        }
        else
        {
            dbMap.EffectSHA256Hash = "";
            dbMap.Effects = 0;
        }

        if (map.ScrollVelocities.Count >= 20)
            dbMap.Effects |= MapEffectType.ScrollVelocity;

        if (!string.IsNullOrEmpty(map.StoryboardFile) && archive.Storyboards.TryGetValue(map.StoryboardFile, out var storyboard))
            dbMap.StoryboardSHA256Hash = MapUtils.GetHash(storyboard.RawContent);
        else
            dbMap.StoryboardSHA256Hash = "";

        dbMap.SHA256Hash = MapUtils.GetHash(map.RawContent);

        dbMap.NotesPerSecond = MapUtils.GetNps(map.HitObjects);
        dbMap.Hits = map.HitObjects.Count(x => x.Type == HitObjectType.Normal && !x.LongNote);
        dbMap.LongNotes = map.HitObjects.Count(x => x.Type == HitObjectType.Normal && x.LongNote);
        dbMap.TickNotes = map.HitObjects.Count(x => x.Type == HitObjectType.Tick);
        dbMap.Landmines = map.HitObjects.Count(x => x.Type == HitObjectType.Landmine);

        dbMap.AccuracyDifficulty = map.AccuracyDifficulty;
        dbMap.HealthDifficulty = map.HealthDifficulty;

        maps.Update(dbMap);
        archive.Dispose();
        return Task.CompletedTask;
    }
}
