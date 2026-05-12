using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using fluxel.Components;
using fluxel.Database;
using fluxel.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace fluxel.Tasks.MapSets;

public class GeneratePreviewTask : IBasicTask
{
    public string Name => $"GeneratePreview({id})";

    private long id { get; }

    public GeneratePreviewTask(long id)
    {
        this.id = id;
    }

    public Task Run(IServiceProvider services)
    {
        var set = services.GetRequiredService<MapManager>().GetSet(id);

        if (set == null)
            throw new ArgumentException($"No set with id {id} was found!");

        var path = $"{Environment.CurrentDirectory}/Assets/map/{id}.zip";
        using var zip = ZipFile.OpenRead(path);

        if (!ServerMapUtils.TryLoadFromZip(zip, out var jsons))
            throw new Exception($"Failed to load mapset {set.ID}!");

        var map = jsons.FirstOrDefault()!;
        var audio = map.AudioFile;
        var ext = Path.GetExtension(audio);

        using var ms = new MemoryStream();
        using var entry = zip.GetEntry(audio)!.Open();
        entry.CopyTo(ms);

        var tempPath = TempUtils.CopyToTemp(ms.ToArray(), ext);
        services.GetRequiredService<PreviewGenerator>().Generate(tempPath, Assets.GetPathForAsset(AssetType.Preview, set.ID.ToString()), map.Metadata.PreviewTime / 1000f);
        return Task.CompletedTask;
    }
}
