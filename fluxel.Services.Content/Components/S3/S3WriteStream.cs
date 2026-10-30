using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Midori.Logging;

namespace fluxel.Services.Content.Components.S3;

public class S3WriteStream : MemoryStream
{
    private readonly IAmazonS3 s3;
    private readonly string bucket;
    private readonly string key;

    private bool performedWrite;

    public S3WriteStream(IAmazonS3 s3, string bucket, string key)
    {
        this.s3 = s3;
        this.bucket = bucket;
        this.key = key;
    }

    public async Task WriteToS3()
    {
        Position = 0;
        performedWrite = true;

        try
        {
            await s3.PutObjectAsync(new PutObjectRequest
            {
                BucketName = bucket,
                Key = key,
                InputStream = this
            });

            performedWrite = true;
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to upload stream to S3 for key '{key}': {ex.Message}", ex);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (!performedWrite)
                Logger.Log($"Unwritten S3 stream! ({bucket}://{key})", level: LogLevel.Warning);
        }

        base.Dispose(disposing);
    }
}
