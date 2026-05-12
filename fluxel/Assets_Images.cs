using System;
using System.IO;
using fluXis.Utils;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace fluxel;

public static partial class Assets
{
    private static ImageSize square { get; } = new(new Size(64), new Size(128), new Size(256), new Size(512));
    private static ImageSize background { get; } = new(new Size(320, 180), new Size(640, 360), new Size(1280, 720), new Size(1920, 1080));
    private static ImageSize banner { get; } = new(new Size(320, 105), new Size(640, 210), new Size(1280, 420), new Size(1920, 640));

    public static string WriteHashedImage(AssetType type, byte[] data)
    {
        try
        {
            var hash = MapUtils.GetHash(data);
            WriteAsset(type, hash, data);
            return hash;
        }
        catch (Exception e)
        {
            logger.Add($"Failed to write image!", LogLevel.Error, e);
            return "";
        }
    }

    public static string WriteAnimatedImage(AssetType type, byte[] data)
    {
        try
        {
            var hash = MapUtils.GetHash(data);
            WriteAsset(type, hash, data, "_a", "gif");

            using var gif = Image.Load<Rgba32>(data);
            using var img = gif.Frames.CloneFrame(0);

            using var ms = new MemoryStream();
            img.SaveAsPng(ms);
            ms.Seek(0, SeekOrigin.Begin);

            var stillBytes = ms.ToArray();
            WriteAsset(type, hash, stillBytes);

            return hash;
        }
        catch (Exception e)
        {
            logger.Add("Failed to write image!", LogLevel.Error, e);
            return "";
        }
    }

    public static void WriteImage(AssetType type, long id, Stream stream)
    {
        try
        {
            if (stream.CanSeek)
                stream.Seek(0, SeekOrigin.Begin);

            var image = Image.Load<Rgba32>(stream);

            var sizing = getImageSize(type);

            if (sizing?.Normal == null)
            {
                logger.Add("Invalid asset type for resize", LogLevel.Error);
                return;
            }

            process(type, id, image, sizing.Small, "-sm");
            process(type, id, image, sizing.Normal, "");
            process(type, id, image, sizing.Large ?? sizing.Normal, "-lg");
            // process(type, id, image, sizing.ExtraLarge ?? sizing.Large ?? sizing.Small, "-xl");
        }
        catch (Exception e)
        {
            logger.Add($"Failed to write image!", LogLevel.Error, e);
        }
    }

    private static void process(AssetType type, long id, Image<Rgba32> image, Size size, string suffix)
    {
        var clone = image.Clone();

        var aspect = (float)size.Width / size.Height;
        var width = size.Width;
        var height = size.Height;

        if (aspect > 1 && clone.Height < size.Height)
        {
            width = (int)(clone.Height * aspect);
            height = clone.Height;
        }
        else if (clone.Width < size.Width)
        {
            width = clone.Width;
            height = (int)(clone.Width / aspect);
        }

        clone.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(width, height),
            Mode = ResizeMode.Crop
        }));

        using var stream = new MemoryStream();
        clone.SaveAsJpeg(stream);

        WriteAsset(type, id, stream, suffix);
    }

    private static ImageSize? getImageSize(AssetType type) => type switch
    {
        AssetType.Background => background, // 16:9
        AssetType.Cover or AssetType.Avatar => square, // 1:1
        AssetType.Banner => banner, // 3:1
        _ => null
    };

    private class ImageSize
    {
        public Size Small { get; }
        public Size Normal { get; }
        public Size? Large { get; }
        public Size? ExtraLarge { get; }

        public ImageSize(Size small, Size normal, Size? large, Size? extraLarge)
        {
            Small = small;
            Normal = normal;
            Large = large;
            ExtraLarge = extraLarge;
        }
    }
}
