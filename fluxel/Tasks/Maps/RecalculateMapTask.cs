using System;
using fluxel.Database.Helpers;
using fluxel.Models.Maps;
using fluxel.Utils;
using fluXis.Online.API.Models.Maps;
using fluXis.Utils;

namespace fluxel.Tasks.Maps;

public class RecalculateMapTask : IBasicTask
{
    public string Name => $"RecalculateMap({id})";

    private long id { get; }

    public RecalculateMapTask(long id)
    {
        this.id = id;
    }

    public void Run()
    {
        var dbMap = MapHelper.Get(id);

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
        dbMap.Hits = map.HitsForPlayer(0);
        dbMap.LongNotes = map.LongNotesForPlayer(0);

        dbMap.PlayerCount = map.PlayerCount;
        dbMap.DualSides.Clear();

        for (int i = 1; i < map.PlayerCount; i++)
        {
            dbMap.DualSides.Add(new Map.MapDualSide
            {
                Hits = map.HitsForPlayer(i),
                LongNotes = map.LongNotesForPlayer(i)
            });
        }

        dbMap.AccuracyDifficulty = map.AccuracyDifficulty;
        dbMap.HealthDifficulty = map.HealthDifficulty;

        MapHelper.Update(dbMap);
        archive.Dispose();
    }
}
