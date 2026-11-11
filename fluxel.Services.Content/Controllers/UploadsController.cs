using System.IO;
using System.IO.Compression;
using System.Linq;
using fluxel.Components;
using fluxel.Components.IPC;
using fluxel.Config;
using fluxel.Models;
using fluxel.Models.Maps;
using fluxel.Models.Maps.Flags;
using fluxel.Models.Users;
using fluxel.Models.Users.Flags;
using fluxel.Services.Content.Components;
using fluxel.Services.Content.Components.S3;
using fluxel.Services.Content.Extensions;
using fluxel.Services.Content.Utils;
using fluXis.IO;
using fluXis.Modes;
using fluXis.Online.API.Models.Maps;
using Microsoft.Extensions.Logging;
using Midori.API.Attributes;
using Midori.API.Components;
using Midori.Logging;
using Midori.Networking;
using osu.Framework.Extensions.EnumExtensions;

namespace fluxel.Services.Content.Controllers;

[Controller("/uploads")]
public class UploadsController
{
    private readonly IStorageProvider storageProvider;
    private readonly ModelTranslator translator;
    private readonly DatabaseContext database;
    private readonly GameModeManager modes;
    private readonly ServerConfig config;
    private readonly Valkey? valkey;

    public UploadsController(DatabaseContext database, ServerConfig config, IStorageProvider storageProvider, GameModeManager modes, ModelTranslator translator, Valkey? valkey = null)
    {
        this.database = database;
        this.config = config;
        this.storageProvider = storageProvider;
        this.modes = modes;
        this.translator = translator;
        this.valkey = valkey;
    }

    [Authenticated, HttpRoute("/mapsets", APIMethod.Post)]
    public APIReturn<APIMapSet> UploadMapSet(
        User auth,
        [Source(ParameterSource.Form)] Stream file
    )
    {
        if (auth.BanFlags.HasFlagFast(UserBanFlag.UploadBan))
            return Returns.Message(HttpStatusCode.Forbidden, "You are banned from uploading mapsets");

        Logger.Log("Checking user limits...", level: LogLevel.Debug);

        var limit = database.Users.GetUploadLimit(
            auth.ID,
            database,
            config.Limits.MaxMapSets,
            config.Limits.IncreasePerPure,
            config.Limits.IncreasePerYear
        );

        if (limit.cur >= limit.max)
            return Returns.Message(HttpStatusCode.Forbidden, "You have reached your upload limit.");
        if (file.Length > MapSet.MAX_PACKAGE_SIZE)
            return Returns.Message(HttpStatusCode.BadRequest, "The file is too large. The maximum file size is 75MB.");

        Logger.Log("Reading as zip...", level: LogLevel.Debug);

        using var zip = new ZipArchive(file);
        var zips = new ZipStorage(zip);
        var set = new MapSet { UserID = auth.ID };

        string? background = null;
        string? cover = null;

        Logger.Log("Checking entries...", level: LogLevel.Debug);

        foreach (var entry in zips.GetFiles(string.Empty))
        {
            var format = modes.GetFormat(zips, Path.GetExtension(entry));
            if (format is null || !format.IsChart(entry)) continue;

            Logger.Log($"Parsing '{entry}'...", level: LogLevel.Debug);

            var chart = format.Parse(entry);
            if (chart is null) return Returns.Message(HttpStatusCode.BadRequest, $"{entry} could not be parsed as a chart.");

            // TODO: check mode

            if (!chart.Validate(out var issue))
                return Returns.Message(HttpStatusCode.BadRequest, $"The map {entry} is invalid. ({issue})");

            if (string.IsNullOrWhiteSpace(background) && !string.IsNullOrWhiteSpace(chart.BackgroundFile))
                background = chart.BackgroundFile;
            if (string.IsNullOrWhiteSpace(cover) && !string.IsNullOrWhiteSpace(chart.CoverFile))
                cover = chart.CoverFile;

            Logger.Log("Writing into database map...", level: LogLevel.Debug);

            var map = new Map { MapSet = set, FileName = entry };
            chart.WriteIntoDatabase(database, map, set.UserID);
            set.Maps.Add(map);
        }

        if (set.Maps.Count == 0)
            return Returns.Message(HttpStatusCode.BadRequest, "The zip file does not contain any valid map files.");

        Logger.Log("Assigning set metadata...", level: LogLevel.Debug);

        var first = set.Maps.First();
        set.Title = first.Title;
        set.TitleRomanized = first.TitleRomanized;
        set.Artist = first.Artist;
        set.ArtistRomanized = first.ArtistRomanized;
        set.Status = MapSetStatus.Unsubmitted;

        Logger.Log("Saving to database...", level: LogLevel.Debug);
        database.MapSets.Add(set);
        database.SaveChanges();

        Logger.Log("Uploading to S3...", level: LogLevel.Debug);
        var storage = storageProvider.CreateStorage("fluxel", string.Empty);
        var setStorage = storage.GetStorageForDirectory(Path.Combine("mapsets", $"{set.ID}"));

        foreach (var entry in zip.Entries)
        {
            using var output = setStorage.GetStream(entry.FullName, FileAccess.Write, FileMode.Create);
            using var stream = entry.Open();
            stream.CopyTo(output);
            if (output is S3WriteStream sws) sws.WriteToS3().Wait();
        }

        Logger.Log("Generating assets...", level: LogLevel.Debug);
        cover ??= background;

        // TODO: generate background and cover
        if (!string.IsNullOrWhiteSpace(background) && zip.TryGetEntry(background, out var backgroundEntry))
        {
        }

        if (!string.IsNullOrWhiteSpace(cover) && zip.TryGetEntry(cover, out var coverEntry))
        {
        }

        Logger.Log("Finishing up...", level: LogLevel.Debug);

        _ = valkey?.Publish(Valkey.MapSetCreate, new ValkeyIDEvent(set.ID));
        return translator.ToAPI(set, mapInclude: MapIncludes.FileName);
    }
}
