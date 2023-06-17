namespace fluxel; 

public static class Assets {
    public static byte[] GetAsset(AssetType type, string id) {
        var prefix = GetType(type);
        var extension = "png";

        var path = $"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}Assets{Path.DirectorySeparatorChar}{prefix}{Path.DirectorySeparatorChar}{id}.{extension}";
        
        if (!File.Exists(path))
            path = $"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}Assets{Path.DirectorySeparatorChar}{prefix}{Path.DirectorySeparatorChar}default.{extension}";

        try {
            return File.ReadAllBytes(path);
        } catch (Exception) {
            Console.WriteLine($"Failed to load asset {path}");
            return Array.Empty<byte>();
        }
    }

    public static void WriteAsset(AssetType type, int id, byte[] data) {
        var prefix = GetType(type);
        var extension = "png";

        var path = $"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}Assets{Path.DirectorySeparatorChar}{prefix}{Path.DirectorySeparatorChar}{id}.{extension}";

        if (File.Exists(path)) File.Delete(path);
        
        var writer = new FileStream(path, FileMode.CreateNew);
        writer.Write(data, 0, data.Length);
        writer.Close();
    }

    private static string GetType(AssetType type) {
        return  type switch {
            AssetType.Avatar => "avatars",
            AssetType.Banner => "banners",
            AssetType.Background => "backgrounds",
            AssetType.Cover => "covers",
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