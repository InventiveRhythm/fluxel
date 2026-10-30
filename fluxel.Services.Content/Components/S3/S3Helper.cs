using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using DotNetEnv;
using Microsoft.Extensions.Logging;

namespace fluxel.Services.Content.Components.S3;

public class S3Helper
{
    private readonly ILogger logger;
    public readonly AmazonS3Client Client;

    public S3Helper(AmazonS3Config config, ILoggerFactory logs)
    {
        logger = logs.CreateLogger("S3");

        Client = new AmazonS3Client(
            Env.GetString("FLUXEL_S3_ACCESS_KEY"),
            Env.GetString("FLUXEL_S3_SECRET_KEY"),
            config
        );
    }

    public async Task<bool> FileExists(string bucket, string name)
    {
        try
        {
            await Client.GetObjectMetadataAsync(new GetObjectMetadataRequest
            {
                BucketName = bucket,
                Key = name
            });

            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task EnsureBucketExists(string name, params LifecycleRule[] rules)
    {
        var buckets = await Client.ListBucketsAsync();
        var exists = buckets.Buckets != null && buckets.Buckets.Exists(x => x.BucketName == name);

        if (exists)
            return;

        await Client.PutBucketAsync(new PutBucketRequest
        {
            BucketName = name,
            UseClientRegion = true
        });

        logger.LogInformation($"Created bucket '{name}'.");

        if (rules.Length == 0)
            return;

        await Client.PutLifecycleConfigurationAsync(new PutLifecycleConfigurationRequest
        {
            BucketName = name,
            Configuration = new LifecycleConfiguration { Rules = [.. rules] }
        });
    }

    public async Task<S3Object[]> GetAllInFolder(string bucket, string path, bool recursive = false)
    {
        if (!path.EndsWith('/'))
            path += '/';

        var all = new List<S3Object>();
        string? cont = null;

        while (true)
        {
            var res = await Client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = bucket,
                Prefix = path == "/" ? string.Empty : path,
                ContinuationToken = cont,
                Delimiter = recursive ? null : "/"
            });

            if (res.S3Objects != null)
                all.AddRange(res.S3Objects);

            if (string.IsNullOrWhiteSpace(res.ContinuationToken))
                break;
        }

        return [.. all];
    }

    public async Task<string[]> GetDirectories(string bucket, string path)
    {
        if (!path.EndsWith('/'))
            path += '/';

        var res = await Client.ListObjectsV2Async(new ListObjectsV2Request
        {
            BucketName = bucket,
            Prefix = path == "/" ? string.Empty : path,
            Delimiter = "/"
        });

        return res.CommonPrefixes?.ToArray() ?? [];
    }
}
