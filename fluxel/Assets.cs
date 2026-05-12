using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.Extensions.Logging;
using Midori.Logging;

namespace fluxel;

public static partial class Assets
{
    private static Logger logger => Logger.GetLogger("Assets");

    private static char separator => Path.DirectorySeparatorChar;

    public const int MAX_IMAGE_SIZE = 3 * 1024 * 1024;

    public static byte[] GetAsset(AssetType type, string id)
    {
        var path = GetPathForAsset(type, id);

        if (!File.Exists(path))
            path = getDefaultPath(type);

        try
        {
            return File.ReadAllBytes(path);
        }
        catch (Exception e)
        {
            logger.Add($"Failed to load asset {path}!", LogLevel.Error, e);
            return Array.Empty<byte>();
        }
    }

    public static FileStream? GetAssetStream(AssetType type, string id, string suffix = "")
    {
        var path = GetPathForAsset(type, id, suffix);

        if (!File.Exists(path))
            path = getDefaultPath(type);

        try
        {
            return File.OpenRead(path);
        }
        catch (Exception e)
        {
            logger.Add($"Failed to load asset {path}!", LogLevel.Error, e);
            return null;
        }
    }

    public static void WriteAsset(AssetType type, long id, MemoryStream stream, string suffix = "")
        => WriteAsset(type, $"{id}", stream, suffix);

    public static void WriteAsset(AssetType type, long id, byte[] data, string suffix = "")
        => WriteAsset(type, $"{id}", data, suffix);

    public static void WriteAsset(AssetType type, string id, MemoryStream stream, string suffix = "")
    {
        if (stream.CanSeek)
            stream.Seek(0, SeekOrigin.Begin);

        WriteAsset(type, id, stream.ToArray(), suffix);
    }

    public static void WriteAsset(AssetType type, string id, byte[] data, string suffix = "", string? extension = null)
    {
        var path = GetPathForAsset(type, id, suffix);

        if (!string.IsNullOrWhiteSpace(extension))
            path = Path.ChangeExtension(path, extension);

        if (File.Exists(path))
            File.Delete(path);

        using var writer = new FileStream(path, FileMode.CreateNew);
        writer.Write(data, 0, data.Length);
        writer.Close();
    }

    public static string GetPathForAsset(AssetType type, string name, string suffix = "")
    {
        var prefix = getType(type);
        string extension;

        if (name.EndsWith("_a"))
            extension = "gif";
        else if (name.EndsWith("_v"))
            extension = "mp4";
        else
            extension = getExtension(type);

        var dir = $"{Directory.GetCurrentDirectory()}{separator}Assets{separator}{prefix}";

        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        return $"{dir}{separator}{name}{suffix}.{extension}";
    }

    private static string getDefaultPath(AssetType type)
    {
        var prefix = getType(type);
        var extension = getExtension(type);
        return $"{Directory.GetCurrentDirectory()}{separator}Assets{separator}{prefix}{separator}default.{extension}";
    }

    private static string getType(AssetType type) => type switch
    {
        AssetType.Achievement => "achievement",
        AssetType.Avatar => "avatar",
        AssetType.Banner => "banner",
        AssetType.Background => "background",
        AssetType.Cover => "cover",
        AssetType.ClubIcon => "club-icon",
        AssetType.ClubBanner => "club-banner",
        AssetType.Map => "map",
        AssetType.Preview => "preview",
        AssetType.Replay => "replay",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    public static bool TryGetType(string str, [NotNullWhen(true)] out AssetType? type)
    {
        type = str switch
        {
            "achievement" => AssetType.Achievement,
            "avatar" => AssetType.Avatar,
            "banner" => AssetType.Banner,
            "background" => AssetType.Background,
            "cover" => AssetType.Cover,
            "club-icon" => AssetType.ClubIcon,
            "club-banner" => AssetType.ClubBanner,
            "map" => AssetType.Map,
            "preview" => AssetType.Preview,
            "replay" => AssetType.Replay,
            _ => null
        };

        return type is not null;
    }

    private static string getExtension(AssetType type) => type switch
    {
        AssetType.Achievement => "png",
        AssetType.Avatar => "png",
        AssetType.Banner => "png",
        AssetType.Background => "jpg",
        AssetType.Cover => "jpg",
        AssetType.ClubIcon => "png",
        AssetType.ClubBanner => "png",
        AssetType.Map => "zip",
        AssetType.Preview => "ogg",
        AssetType.Replay => "frp",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
}

public enum AssetType
{
    Achievement,
    Avatar,
    Banner,
    Background,
    Cover,
    ClubIcon,
    ClubBanner,
    Map,
    Preview,
    Replay
}
