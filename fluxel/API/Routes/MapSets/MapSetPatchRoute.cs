using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using fluxel.API.Components;
using fluxel.Constants;
using fluxel.Database.Extensions;
using fluxel.Database.Helpers;
using fluxel.Models.Maps;
using fluxel.Tasks.Maps;
using fluxel.Utils;
using fluXis.Map;
using fluXis.Online.API.Models.Maps.Modding;
using fluXis.Utils;
using Midori.API.Components.Interfaces;
using Midori.Networking;
using osu.Framework.Extensions.IEnumerableExtensions;
using JsonUtils = Midori.Utils.JsonUtils;

namespace fluxel.API.Routes.MapSets;

public class MapSetPatchRoute : IFluxelAPIRoute, INeedsAuthorization
{
    public string RoutePath => "/mapset/:id";
    public HttpMethod Method => HttpMethod.Patch;

    public async Task Handle(FluxelAPIInteraction interaction)
    {
        if (!interaction.TryGetLongParameter("id", out var id))
        {
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, ResponseStrings.InvalidParameter("id", "long"));
            return;
        }

        var set = MapSetHelper.Get(id);

        if (set == null)
        {
            await interaction.ReplyMessage(HttpStatusCode.NotFound, ResponseStrings.MapSetNotFound);
            return;
        }

        if (set.CreatorID != interaction.User.ID)
        {
            await interaction.ReplyMessage(HttpStatusCode.Forbidden, "You are not the creator of this mapset.");
            return;
        }

        // map ranked
        if (set.Status >= MapStatus.Pure)
        {
            await interaction.ReplyMessage(HttpStatusCode.Forbidden, "You cannot update a purified mapset.");
            return;
        }

        if (!interaction.TryGetFile("file", out var file))
        {
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, "No file was provided.");
            return;
        }

        using var stream = new MemoryStream();
        await file.Data.CopyToAsync(stream);

        if (stream.Length > MapSetHelper.MAX_PACKAGE_SIZE)
        {
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, "The file is too large. The maximum file size is 75MB.");
            return;
        }

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
            var json = await new StreamReader(entry.Open()).ReadToEndAsync();
            var mapJson = JsonUtils.Deserialize<MapInfo>(json);

            if (mapJson == null || !mapJson.Validate(out _))
            {
                await interaction.ReplyMessage(HttpStatusCode.BadRequest, "The file " + entry.Name + " is not a valid map file.");
                return;
            }

            fileNames.Add(entry.FullName);

            var hash = MapUtils.GetHash(json);
            var mapper = UserHelper.Get(mapJson.Metadata.Mapper) ?? interaction.User;

            var existing = set.MapsList.FirstOrDefault(m => m.FileName == entry.FullName);

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
                    await background.Open().CopyToAsync(backgroundStream);
                    hasBackground = true;
                }
            }

            if (hasCover || mapJson.CoverFile == "")
                continue;

            var cover = zip.GetEntry(mapJson.CoverFile);

            if (cover == null)
                continue;

            await cover.Open().CopyToAsync(coverStream);
            hasCover = true;
        }

        var newSplit = new List<long>();

        // delete old maps
        foreach (var map in set.MapsList)
        {
            if (fileNames.Contains(map.FileName))
                newSplit.Add(map.ID);
            else
                MapHelper.Remove(map);
        }

        foreach (var updated in updatedMaps)
        {
            var original = set.MapsList.FirstOrDefault(x => x.ID == updated.ID) ?? throw new InvalidOperationException("Attempting to update a non-existent map!");

            /*if (original.FullHash != updated.FullHash)
                MapHelper.ClearVotes(original.ID);*/

            MapHelper.Update(updated);
        }

        // add new maps
        foreach (var map in newMaps)
        {
            MapHelper.Add(map);
            newSplit.Add(map.ID);
        }

        // write file to disk
        Assets.WriteAsset(AssetType.Map, set.ID, stream);

        if (!hasCover && hasBackground)
        {
            backgroundStream.Seek(0, SeekOrigin.Begin);
            await backgroundStream.CopyToAsync(coverStream);
            hasCover = true;
        }

        // update background
        if (hasBackground)
            Assets.WriteImage(AssetType.Background, set.ID, backgroundStream);
        if (hasCover)
            Assets.WriteImage(AssetType.Cover, set.ID, coverStream);

        var first = set.MapsList.First();
        set.Title = first.Title;
        set.TitleRomanized = first.TitleRomanized;
        set.Artist = first.Artist;
        set.ArtistRomanized = first.ArtistRomanized;
        set.Maps = newSplit;
        set.LastUpdated = DateTimeOffset.Now;

        MapSetHelper.Update(set);

        await interaction.Reply(HttpStatusCode.OK, set.ToAPI(mapInclude: MapIncludes.FileName));
        set.Maps.ForEach(m => ServerHost.Instance.Scheduler.Schedule(new RecalculateMapTask(m)));

        if (MapSetHelper.HasActions(set.ID) && set.Status == MapStatus.Pending)
            MapSetHelper.CreateModAction(set.ID, interaction.UserID, APIModdingActionType.Update);
    }
}
