using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using fluxel.Components.Tasks;
using fluxel.Models;
using fluxel.Services.Content.Components;
using fluxel.Services.Content.Components.S3;
using fluXis.Modes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace fluxel.Services.Content.Tasks.Previews;

public class GeneratePreviewTask : IBasicTask
{
    public string Name => $"GeneratePreview({id})";

    private long id { get; }

    public GeneratePreviewTask(long id)
    {
        this.id = id;
    }

    public async Task Run(IServiceProvider services)
    {
        var database = services.GetRequiredService<DatabaseContext>();
        var set = database.MapSets.Include(x => x.Maps).FirstOrDefault(x => x.ID == id);
        if (set is null) throw new ArgumentException($"No set with id {id} was found!");

        var path = Path.Combine("mapsets", id.ToString());
        var s3 = services.GetRequiredService<S3Helper>();
        var storage = new S3Storage(s3, "fluxel", path);

        var map = set.Maps.First();
        var modes = services.GetRequiredService<GameModeManager>();
        var format = modes.GetFormat(storage, Path.GetExtension(map.FileName));
        if (format is null) throw new Exception($"Unknown format for file '{map.FileName}'.");

        var playable = format.Parse(map.FileName);
        if (playable is null) throw new Exception($"Failed to parse file '{map.FileName}'.");

        if (!storage.Exists(playable.AudioFile))
            return;

        float time = playable.PreviewTime;
        var len = 0f;

        while (len < 15000)
        {
            var beat = Math.Clamp(playable.GetTimingPoint(time).MsPerBeat * 4, 10, 15000);
            len += beat;
            time += beat;
        }

        using var result = await services.GetRequiredService<PreviewGenerator>().Generate(
            storage.GetStream(playable.AudioFile, FileAccess.Read, FileMode.Open),
            playable.PreviewTime / 1000f,
            len / 1000f
        );

        var root = new S3Storage(s3, "fluxel");
        using var output = root.GetStream(Path.Combine("previews", $"{set.ID}.ogg"), FileAccess.Write, FileMode.Create);
        await result.CopyToAsync(output);
        if (output is S3WriteStream sws) await sws.WriteToS3();
    }
}
