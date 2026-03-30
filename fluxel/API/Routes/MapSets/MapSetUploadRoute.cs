using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using fluxel.API.Components;
using fluxel.Database.Extensions;
using fluxel.Database.Helpers;
using fluxel.Models.Maps;
using fluxel.Models.Users;
using fluxel.Tasks.Maps;
using fluxel.Tasks.MapSets;
using fluxel.Utils;
using fluXis.Map;
using fluXis.Utils;
using Midori.API.Components.Interfaces;
using Midori.Networking;
using JsonUtils = Midori.Utils.JsonUtils;

namespace fluxel.API.Routes.MapSets;

public class MapSetUploadRoute : IFluxelAPIRoute, INeedsAuthorization
{
    public string RoutePath => "/mapsets";
    public HttpMethod Method => HttpMethod.Post;

    public async Task Handle(FluxelAPIInteraction interaction)
    {
        if (interaction.User.HasFlag(UserBanFlag.UploadBan))
        {
            await interaction.ReplyMessage(HttpStatusCode.Forbidden, "You are banned from uploading mapsets.");
            return;
        }

        if (!checkLimit(interaction))
        {
            await interaction.ReplyMessage(HttpStatusCode.Forbidden, "You have reached your upload limit.");
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

        var set = new MapSet
        {
            CreatorID = interaction.User.ID
        };

        var maps = new List<Map>();

        using var backgroundStream = new MemoryStream();
        var hasBackground = false;

        using var coverStream = new MemoryStream();
        var hasCover = false;

        foreach (var entry in zip.Entries.Where(e => e.FullName.EndsWith(".fsc")))
        {
            var json = await new StreamReader(entry.Open()).ReadToEndAsync();
            var mapJson = JsonUtils.Deserialize<MapInfo>(json);

            var issue = "";

            if (mapJson == null || !mapJson.Validate(out issue))
            {
                await interaction.ReplyMessage(HttpStatusCode.BadRequest, $"The file {entry.Name} is not a valid map file. ({issue})");
                return;
            }

            if (!hasBackground)
            {
                try
                {
                    var background = zip.GetEntry(mapJson.BackgroundFile);

                    if (background != null)
                    {
                        await background.Open().CopyToAsync(backgroundStream);
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
                        await cover.Open().CopyToAsync(coverStream);
                        hasCover = true;
                    }
                }
                catch
                {
                    // ignored
                }
            }

            var hash = MapUtils.GetHash(json);
            var mapper = UserHelper.Get(mapJson.Metadata.Mapper) ?? interaction.User;

            var effects = zip.ReadFile(mapJson.EffectFile, out var e) ? e : "";
            var storyboard = zip.ReadFile(mapJson.StoryboardFile, out var s) ? s : "";

            var map = ServerMapUtils.CreateFromJson(mapJson, 0, 0, entry.FullName, hash, mapper.ID, effects, storyboard);
            maps.Add(map);
        }

        if (maps.Count == 0)
        {
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, "The zip file does not contain any valid map files.");
            return;
        }

        var first = maps.First();
        set.Title = first.Title;
        set.TitleRomanized = first.TitleRomanized;
        set.Artist = first.Artist;
        set.ArtistRomanized = first.ArtistRomanized;
        set.Status = 0;

        if (!hasCover && hasBackground)
        {
            backgroundStream.Seek(0, SeekOrigin.Begin);
            await backgroundStream.CopyToAsync(coverStream);
            hasCover = true;
        }

        foreach (var map in maps)
            MapHelper.Add(map);

        set.Maps = maps.Select(m => m.ID);
        MapSetHelper.Add(set);

        Assets.WriteAsset(AssetType.Map, set.ID, stream);

        maps.ForEach(m =>
        {
            m.SetID = set.ID;
            MapHelper.Update(m);
        });

        if (hasBackground)
            Assets.WriteImage(AssetType.Background, set.ID, backgroundStream);
        if (hasCover)
            Assets.WriteImage(AssetType.Cover, set.ID, coverStream);

        await interaction.Reply(HttpStatusCode.OK, set.ToAPI(mapInclude: MapIncludes.FileName));

        Events.UploadMap(set.ID);
        maps.ForEach(m => ServerHost.Instance.Scheduler.Schedule(new RecalculateMapTask(m.ID)));
        ServerHost.Instance.Scheduler.Schedule(new GeneratePreviewTask(set.ID));
    }

    private bool checkLimit(FluxelAPIInteraction interaction)
    {
        var current = MapSetHelper.GetUploadedCount(interaction.UserID, MapSetHelper.UploadLimitStartDate);
        var maximum = MapSetHelper.GetUploadLimit(interaction.UserID);

        return current < maximum;
    }
}
