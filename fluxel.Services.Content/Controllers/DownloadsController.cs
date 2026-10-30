using System.IO;
using System.IO.Compression;
using Amazon.S3.Model;
using fluxel.Database.Extensions;
using fluxel.Models;
using fluxel.Services.Content.Components.S3;
using Midori.API.Attributes;
using Midori.API.Components;

namespace fluxel.Services.Content.Controllers;

[Controller("/downloads")]
public class DownloadsController
{
    private readonly DatabaseContext database;
    private readonly S3Helper s3;

    public DownloadsController(DatabaseContext database, S3Helper s3)
    {
        this.database = database;
        this.s3 = s3;
    }

    [Authenticated, HttpRoute("/mapsets/:id")]
    [ReturnsMime("application/zip")]
    public APIReturn<Stream> DownloadMapSet(long id)
    {
        if (!database.MapSets.TryFind(out var set, id))
            return Returns.NotFound("mapset");

        var prefix = $"mapsets/{set.ID}/";
        var files = s3.GetAllInFolder("fluxel", prefix, true).Result;

        var stream = new MemoryStream();
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create, true);

        foreach (var file in files)
        {
            using var res = s3.Client.GetObjectAsync(new GetObjectRequest
            {
                BucketName = file.BucketName,
                Key = file.Key
            }).Result;

            var entry = archive.CreateEntry(Path.GetRelativePath(prefix, file.Key), CompressionLevel.Optimal);
            using var es = entry.Open();
            res.ResponseStream.CopyTo(es);
        }

        return stream;
    }
}
