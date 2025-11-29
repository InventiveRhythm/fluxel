using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using fluxel.Models.Maps;
using fluxel.Models.Maps.Json;
using fluXis.Map;
using fluXis.Storyboards;
using fluXis.Utils;
using Midori.Logging;
using JsonUtils = Midori.Utils.JsonUtils;

namespace fluxel.Utils;

public static class ServerMapUtils
{
    public static Map CreateFromJson(MapInfo json, long id, long set, string entry, string hash, long mapper, string effects, string storyboard)
    {
        var map = new Map
        {
            ID = id,
            SetID = set,
            FileName = entry,
            SHA256Hash = hash,
            EffectSHA256Hash = string.IsNullOrEmpty(effects) ? "" : MapUtils.GetHash(effects),
            StoryboardSHA256Hash = string.IsNullOrEmpty(storyboard) ? "" : MapUtils.GetHash(storyboard),
            MapperID = mapper,
            Title = json.Metadata.Title,
            TitleRomanized = json.Metadata.TitleRomanized,
            Artist = json.Metadata.Artist,
            ArtistRomanized = json.Metadata.ArtistRomanized,
            Source = json.Metadata.AudioSource,
            Tags = json.Metadata.Tags,
            BPM = json.TimingPoints.First().BPM,
            DifficultyName = json.Metadata.Difficulty,
            Mode = json.SinglePlayerKeyCount,
            Length = (int)json.HitObjects.Max(h => h.Time),
            Hits = json.HitsForPlayer(0),
            LongNotes = json.LongNotesForPlayer(0),
            NotesPerSecond = MapUtils.GetNps(json.HitObjects), //TODO: distinct nps for each dual sides?
            PlayerCount = json.PlayerCount
        };

        for (int i = 1; i < json.PlayerCount; ++i)
        {
            map.DualSides.Add(new()
            {
                Hits = json.HitsForPlayer(i),
                LongNotes = json.LongNotesForPlayer(i)
            });
        }

        return map;
    }

    public static bool ReadFile(this ZipArchive archive, string? name, [NotNullWhen(true)] out string? content)
    {
        content = "";

        try
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            var entry = archive.GetEntry(name);

            if (entry is null)
                return false;

            using var stream = entry.Open();
            content = new StreamReader(stream).ReadToEnd();
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to read file from archive!");
            return false;
        }
    }

    public static bool OpenArchive(string path, out MapSetArchive archive)
    {
        archive = new MapSetArchive(ZipFile.OpenRead(path));

        foreach (var entry in archive.Archive.Entries)
        {
            var extension = Path.GetExtension(entry.FullName);
            var stream = entry.Open();

            switch (extension)
            {
                case ".fsc":
                {
                    using var reader = new StreamReader(stream);
                    var json = reader.ReadToEnd();

                    if (!JsonUtils.TryDeserialize<MapInfo>(json, out var map))
                        return false;

                    map!.RawContent = json;
                    map.FileName = entry.FullName;
                    archive.Maps.Add(entry.FullName, map);
                    break;
                }

                case ".ffx":
                case ".fse":
                {
                    using var reader = new StreamReader(stream);
                    var json = reader.ReadToEnd();

                    var events = MapEvents.Load<MapEvents>(json);
                    archive.Events.Add(entry.FullName, events);
                    break;
                }

                case ".fsb":
                {
                    using var reader = new StreamReader(stream);
                    var json = reader.ReadToEnd();

                    if (!JsonUtils.TryDeserialize<Storyboard>(json, out var sb))
                        return false;

                    sb!.RawContent = json;
                    archive.Storyboards.Add(entry.FullName, sb);
                    break;
                }

                default:
                    archive.Other.Add(entry.FullName, stream);
                    break;
            }
        }

        return archive.Maps.Count != 0;
    }

    public static bool TryLoadFromZip(string path, out List<MapInfo> maps)
    {
        var zip = ZipFile.OpenRead(path);
        return TryLoadFromZip(zip, out maps);
    }

    public static bool TryLoadFromZip(ZipArchive zip, out List<MapInfo> maps)
    {
        maps = new List<MapInfo>();

        foreach (var entry in zip.Entries)
        {
            if (!entry.FullName.EndsWith(".fsc")) continue;

            using var stream = entry.Open();
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();

            if (!JsonUtils.TryDeserialize<MapInfo>(json, out var mapJson))
                return false;

            mapJson!.FileName = entry.FullName;
            maps.Add(mapJson);
        }

        return maps.Count > 0;
    }
}
