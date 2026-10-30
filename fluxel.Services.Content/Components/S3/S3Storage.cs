using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Midori.Logging;
using osu.Framework.Platform;

namespace fluxel.Services.Content.Components.S3;

public class S3Storage : Storage
{
    private readonly string bucket;
    private readonly string basePath;

    private readonly S3Helper s3;

    public S3Storage(S3Helper s3, string bucket, string path = "")
        : base($"{bucket}://{path}")
    {
        s3.EnsureBucketExists(bucket).Wait();

        this.s3 = s3;
        this.bucket = bucket;
        basePath = path;
    }

    public override string GetFullPath(string path, bool createIfNotExisting = false)
    {
        if (createIfNotExisting)
            Logger.Log("Used 'createIfNotExisting' in S3Storage! This won't actually do anything.", level: LogLevel.Warning);

        return Path.Combine(basePath, path);
    }

    public override bool Exists(string path)
        => s3.FileExists(bucket, Path.Combine(basePath, path)).Result;

    public override bool ExistsDirectory(string path) => true;

    public override void DeleteDirectory(string path)
    {
        // TODO
    }

    public override void Delete(string path)
    {
        // TODO
    }

    public override IEnumerable<string> GetDirectories(string path)
    {
        var results = s3.GetDirectories(bucket, Path.Combine(basePath, path)).Result;
        return results.Select(x => Path.GetRelativePath(basePath, x).TrimEnd('/'));
    }

    public override IEnumerable<string> GetFiles(string path, string pattern = "*")
    {
        var results = s3.GetAllInFolder(bucket, Path.Combine(basePath, path)).Result;
        return results.Select(x => Path.GetRelativePath(basePath, x.Key));
    }

    public override void Move(string from, string to)
    {
    }

    public override Stream GetStream(string path, FileAccess access = FileAccess.Read, FileMode mode = FileMode.OpenOrCreate)
    {
        path = GetFullPath(path);

        if (access == FileAccess.Read || mode == FileMode.Open)
        {
            try
            {
                var response = s3.Client.GetObjectAsync(new GetObjectRequest
                {
                    BucketName = bucket,
                    Key = path
                }).Result;

                return response.ResponseStream;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                throw new FileNotFoundException($"File not found in S3 at path: {path}", ex);
            }
        }

        if (access == FileAccess.Write || mode == FileMode.Create || mode == FileMode.OpenOrCreate)
            return new S3WriteStream(s3.Client, bucket, path);

        throw new NotSupportedException($"FileAccess '{access}' with FileMode '{mode}' is not supported directly.");
    }

    public override bool OpenFileExternally(string filename) => false;
    public override bool PresentFileExternally(string filename) => false;
}
