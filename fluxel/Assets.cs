namespace fluxel;

public static class Assets {
    public static byte[] GetAsset(AssetType type, string id) {
        var prefix = getType(type);
        const string extension = "png";

        var path = $"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}Assets{Path.DirectorySeparatorChar}{prefix}{Path.DirectorySeparatorChar}{id}.{extension}";

        if (!File.Exists(path))
            path = $"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}Assets{Path.DirectorySeparatorChar}{prefix}{Path.DirectorySeparatorChar}default.{extension}";

        try {
            return File.ReadAllBytes(path);
        }
        catch (Exception) {
            Logger.Log($"Failed to load asset {path}", LogLevel.Error);
            return Array.Empty<byte>();
        }
    }

    public static void WriteAsset(AssetType type, long id, byte[] data) {
        var prefix = getType(type);
        const string extension = "png";

        var path = $"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}Assets{Path.DirectorySeparatorChar}{prefix}{Path.DirectorySeparatorChar}{id}.{extension}";

        if (File.Exists(path)) File.Delete(path);

        var writer = new FileStream(path, FileMode.CreateNew);
        writer.Write(data, 0, data.Length);
        writer.Close();
    }

    private static string getType(AssetType type) {
        return  type switch {
            AssetType.Avatar => "Avatars",
            AssetType.Banner => "Banners",
            AssetType.Background => "Backgrounds",
            AssetType.Cover => "Covers",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}

public enum AssetType {
    Avatar,
    Banner,
    Background,
    Cover
}
