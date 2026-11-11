using System;
using System.Linq;
using System.Reflection;
using fluxel.Models;
using fluxel.Models.Maps;
using fluXis.Map;
using fluXis.Map.Structures;
using fluXis.Map.Structures.Bases;
using fluXis.Map.Structures.Events;
using rhym;
using ResourceLocation = rhym.ResourceLocation;

namespace fluxel.Services.Content.Utils;

public static class ContentMapHelper
{
    public static void WriteIntoDatabase(this PlayableMap chart, DatabaseContext ctx, Map map, long uploader)
    {
        map.SHA256Hash = chart.ChartHash;
        map.EventSHA256Hash = string.IsNullOrWhiteSpace(chart.EventsHash) ? null : chart.EventsHash;
        map.StoryboardSHA256Hash = string.IsNullOrWhiteSpace(chart.StoryboardHash) ? null : chart.StoryboardHash;

        map.MapperIDs =
        [
            .. chart.Creator
                    .Split(",", StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).First().Trim())
                    .Select(x => ctx.Users.ByUsername(x)?.ID)
                    .OfType<long>()
        ];

        if (map.MapperIDs.Length == 0)
            map.MapperIDs = [uploader];

        map.DifficultyName = chart.Difficulty;
        map.AccuracyDifficulty = chart.AccuracyDifficulty;
        map.HealthDifficulty = chart.HealthDifficulty;

        map.Mode = chart.Mode;

        map.Title = chart.Title;
        map.TitleRomanized = string.IsNullOrWhiteSpace(chart.TitleRomanized) ? null : chart.TitleRomanized;
        map.Artist = chart.Artist;
        map.ArtistRomanized = string.IsNullOrWhiteSpace(chart.ArtistRomanized) ? null : chart.ArtistRomanized;
        map.Sources = chart.Sources;
        map.Tags = chart.Tags;

        var timings = chart.ObjectsOfType<TimingPoint>();
        if (timings.Length == 0) timings = [new TimingPoint()];
        var grouped = timings.GroupBy(x => x.BPM).ToArray();
        map.BPM = grouped.MaxBy(x => x.Count())!.Key;
        map.BPMMin = grouped.MinBy(x => x.Key)!.Key;
        map.BPMMax = grouped.MaxBy(x => x.Key)!.Key;

        map.Length = (long)(chart.EndTime - chart.StartTime);

        var hits = chart.ObjectsOfType<HitObject>();
        map.HitObjectCounts = hits.GroupBy(getLocationFor).ToDictionary(x => x.Key, x => x.LongCount());

        var events = chart.ObjectsOfType<IMapEvent>();
        map.EventTags = [.. events.GroupBy(getLocationFor).Select(x => x.Key)];
        var svs = chart.ObjectsOfType<ScrollVelocity>();
        if (svs.Length >= 20) map.EventTags = [.. map.EventTags, getLocationFor(svs[0])];

        // TODO: accent color
    }

    private static ResourceLocation getLocationFor(ITimedObject obj)
    {
        var type = obj.GetType();
        var attr = type.GetCustomAttribute<ResourceLocationAttribute>();
        return attr?.Location ?? new ResourceLocation("flux", "unknown");
    }
}
