using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using fluxel.Components;
using fluxel.Database;
using fluxel.Database.Extensions;
using fluxel.Models.Maps;
using fluxel.Models.Users;
using fluxel.Tasks;
using fluxel.Tasks.Maps;
using fluxel.Tasks.MapSets;
using fluxel.Utils;
using fluXis.Map;
using fluXis.Online.API.Models.Maps;
using fluXis.Online.API.Models.Maps.Modding;
using fluXis.Utils;
using Midori.API.Attributes;
using Midori.API.Components;
using Midori.Networking;
using osu.Framework.Extensions.IEnumerableExtensions;
using JsonUtils = Midori.Utils.JsonUtils;

namespace fluxel.API.Controllers.Maps;

[Controller("/mapsets")]
public class MapSetsUploadController
{
    private readonly MapManager maps;
    private readonly UserManager users;
    private readonly TaskRunner tasks;
    private readonly ModelTranslator translator;
    private readonly ServerEvents events;

    public MapSetsUploadController(MapManager maps, UserManager users, TaskRunner tasks, ModelTranslator translator, ServerEvents events)
    {
        this.maps = maps;
        this.users = users;
        this.tasks = tasks;
        this.translator = translator;
        this.events = events;
    }

    [Authenticated]
    [HttpRoute("/", APIMethod.Post)]
    public APIReturn<APIMapSet> Upload(User auth, [Source(ParameterSource.Form)] Stream file)
    {
        if (auth.HasFlag(UserBanFlag.UploadBan))
            return Returns.Message(HttpStatusCode.Forbidden, "You are banned from uploading mapsets.");

        if (!checkLimit(auth))
            return Returns.Message(HttpStatusCode.Forbidden, "You have reached your upload limit.");

        using var stream = new MemoryStream();
        file.CopyTo(stream);

        if (stream.Length > MapManager.MAX_PACKAGE_SIZE)
            return Returns.Message(HttpStatusCode.BadRequest, "The file is too large. The maximum file size is 75MB.");

        using var zip = new ZipArchive(stream);

        var set = new MapSet
        {
            CreatorID = auth.ID
        };

        var mapList = new List<Map>();

        using var backgroundStream = new MemoryStream();
        var hasBackground = false;

        using var coverStream = new MemoryStream();
        var hasCover = false;

        foreach (var entry in zip.Entries.Where(e => e.FullName.EndsWith(".fsc")))
        {
            var json = new StreamReader(entry.Open()).ReadToEnd();
            var mapJson = JsonUtils.Deserialize<MapInfo>(json);

            var issue = "";

            if (mapJson == null || !mapJson.Validate(out issue))
                return Returns.Message(HttpStatusCode.BadRequest, $"The file {entry.Name} is not a valid map file. ({issue})");

            if (!hasBackground)
            {
                try
                {
                    var background = zip.GetEntry(mapJson.BackgroundFile);

                    if (background != null)
                    {
                        background.Open().CopyTo(backgroundStream);
                        hasBackground = true;
                    }
                }
                catch
                {
                    // ignored
                }
            }

            if (!hasCover)
            {
                try
                {
                    var cover = zip.GetEntry(mapJson.CoverFile);

                    if (cover != null)
                    {
                        cover.Open().CopyTo(coverStream);
                        hasCover = true;
                    }
                }
                catch
                {
                    // ignored
                }
            }

            var hash = MapUtils.GetHash(json);
            var mapper = users.Get(mapJson.Metadata.Mapper) ?? auth;

            var effects = zip.ReadFile(mapJson.EffectFile, out var e) ? e : "";
            var storyboard = zip.ReadFile(mapJson.StoryboardFile, out var s) ? s : "";

            var map = ServerMapUtils.CreateFromJson(mapJson, 0, 0, entry.FullName, hash, mapper.ID, effects, storyboard);
            mapList.Add(map);
        }

        if (mapList.Count == 0)
            return Returns.Message(HttpStatusCode.BadRequest, "The zip file does not contain any valid map files.");

        var first = mapList.First();
        set.Title = first.Title;
        set.TitleRomanized = first.TitleRomanized;
        set.Artist = first.Artist;
        set.ArtistRomanized = first.ArtistRomanized;
        set.Status = 0;

        if (!hasCover && hasBackground)
        {
            backgroundStream.Seek(0, SeekOrigin.Begin);
            backgroundStream.CopyTo(coverStream);
            hasCover = true;
        }

        foreach (var map in mapList)
            maps.Add(map);

        set.Maps = mapList.Select(m => m.ID);
        maps.Add(set);

        Assets.WriteAsset(AssetType.Map, set.ID, stream);

        mapList.ForEach(m =>
        {
            m.SetID = set.ID;
            maps.Update(m);
        });

        if (hasBackground)
            Assets.WriteImage(AssetType.Background, set.ID, backgroundStream);
        if (hasCover)
            Assets.WriteImage(AssetType.Cover, set.ID, coverStream);

        events.UploadMap(set.ID);
        mapList.ForEach(m => tasks.Schedule(new RecalculateMapTask(m.ID)));
        tasks.Schedule(new GeneratePreviewTask(set.ID));

        return translator.ToAPI(set, mapInclude: MapIncludes.FileName);
    }

    [Authenticated]
    [HttpRoute("/:id", APIMethod.Patch)]
    public APIReturn<APIMapSet> Update(User auth, long id, [Source(ParameterSource.Form)] Stream file)
    {
        var set = maps.GetSet(id);
        if (set == null) return Returns.NotFound();

        if (set.CreatorID != auth.ID)
            return Returns.Message(HttpStatusCode.Forbidden, "You are not the creator of this mapset.");

        // map ranked
        if (set.Status >= MapStatus.Pure)
            return Returns.Message(HttpStatusCode.Forbidden, "You cannot update a purified mapset.");

        using var stream = new MemoryStream();
        file.CopyTo(stream);

        if (stream.Length > MapManager.MAX_PACKAGE_SIZE)
            return Returns.Message(HttpStatusCode.BadRequest, "The file is too large. The maximum file size is 75MB.");

        using var zip = new ZipArchive(stream);

        var fileNames = new List<string>();
        var newMaps = new List<Map>();
        var updatedMaps = new List<Map>();

        using var backgroundStream = new MemoryStream();
        var hasBackground = false;

        using var coverStream = new MemoryStream();
        var hasCover = false;

        foreach (var entry in zip.Entries.Where(e => e.FullName.EndsWith(".fsc")))
        {
            var json = new StreamReader(entry.Open()).ReadToEnd();
            var mapJson = JsonUtils.Deserialize<MapInfo>(json);

            if (mapJson == null || !mapJson.Validate(out _))
                return Returns.Message(HttpStatusCode.BadRequest, "The file " + entry.Name + " is not a valid map file.");

            fileNames.Add(entry.FullName);

            var hash = MapUtils.GetHash(json);
            var mapper = users.Get(mapJson.Metadata.Mapper) ?? auth;

            var existing = set.GetMaps(translator.Cache).FirstOrDefault(m => m.FileName == entry.FullName);

            var effects = zip.ReadFile(mapJson.EffectFile, out var e) ? e : "";
            var storyboard = zip.ReadFile(mapJson.StoryboardFile, out var s) ? s : "";

            var map = ServerMapUtils.CreateFromJson(mapJson, existing?.ID ?? 0, set.ID, entry.FullName, hash, mapper.ID, effects, storyboard);

            if (existing is null)
                newMaps.Add(map);
            else
                updatedMaps.Add(map);

            if (!hasBackground && mapJson.BackgroundFile != "")
            {
                var background = zip.GetEntry(mapJson.BackgroundFile);

                if (background != null)
                {
                    background.Open().CopyTo(backgroundStream);
                    hasBackground = true;
                }
            }

            if (hasCover || mapJson.CoverFile == "")
                continue;

            var cover = zip.GetEntry(mapJson.CoverFile);

            if (cover == null)
                continue;

            cover.Open().CopyTo(coverStream);
            hasCover = true;
        }

        var newSplit = new List<long>();

        // delete old maps
        foreach (var map in set.GetMaps(translator.Cache))
        {
            if (fileNames.Contains(map.FileName))
                newSplit.Add(map.ID);
            else
                maps.RemoveMap(map);
        }

        foreach (var updated in updatedMaps)
        {
            var original = set.GetMaps(translator.Cache).FirstOrDefault(x => x.ID == updated.ID) ?? throw new InvalidOperationException("Attempting to update a non-existent map!");

            /*if (original.FullHash != updated.FullHash)
                MapHelper.ClearVotes(original.ID);*/

            maps.Update(updated);
        }

        // add new maps
        foreach (var map in newMaps)
        {
            maps.Add(map);
            newSplit.Add(map.ID);
        }

        // write file to disk
        Assets.WriteAsset(AssetType.Map, set.ID, stream);

        if (!hasCover && hasBackground)
        {
            backgroundStream.Seek(0, SeekOrigin.Begin);
            backgroundStream.CopyTo(coverStream);
            hasCover = true;
        }

        // update background
        if (hasBackground)
            Assets.WriteImage(AssetType.Background, set.ID, backgroundStream);
        if (hasCover)
            Assets.WriteImage(AssetType.Cover, set.ID, coverStream);

        var first = set.GetMaps(translator.Cache).First();
        set.Title = first.Title;
        set.TitleRomanized = first.TitleRomanized;
        set.Artist = first.Artist;
        set.ArtistRomanized = first.ArtistRomanized;
        set.Maps = newSplit;
        set.LastUpdated = DateTimeOffset.Now;

        maps.Update(set);

        set.Maps.ForEach(m => tasks.Schedule(new RecalculateMapTask(m)));

        if (maps.HasActions(set.ID) && set.Status == MapStatus.Pending)
            maps.CreateModAction(set.ID, auth.ID, APIModdingActionType.Update);

        return translator.ToAPI(set, mapInclude: MapIncludes.FileName);
    }

    private bool checkLimit(User user)
    {
        var current = maps.CountUploaded(user.ID, maps.UploadLimitStartDate);
        var maximum = maps.GetUploadLimit(user.ID);

        return current < maximum;
    }
}
