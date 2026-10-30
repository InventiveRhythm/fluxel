using System.IO;
using fluxel.Services.Content.Components;
using fluxel.Services.Content.Components.S3;
using Midori.API.Attributes;
using Midori.API.Components;

namespace fluxel.Services.Content.Controllers;

[Controller("/content/testing")]
public class TestingController
{
    private readonly S3Helper s3;
    private readonly PreviewGenerator previews;

    public TestingController(S3Helper s3, PreviewGenerator previews)
    {
        this.s3 = s3;
        this.previews = previews;
    }

    [HttpRoute("/")]
    public APIReturn<string[]> Testing()
    {
        var storage = new S3Storage(s3, "fluxel");
        using var input = storage.GetStream("preview-input.mp3", FileAccess.Read, FileMode.Open);
        using var result = previews.Generate(input, 229480 / 1000f).Result;
        using var output = storage.GetStream("preview-output.ogg", FileAccess.Write, FileMode.Create);

        result.CopyTo(output);
        (output as S3WriteStream)?.WriteToS3().Wait();

        return Returns.Okay();
    }
}
