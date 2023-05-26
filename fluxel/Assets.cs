namespace fluxel; 

public static class Assets {
    public static byte[] GetAsset(AssetType type, string id) {
        var prefix = type switch {
            AssetType.Avatar => "avatars",
            AssetType.Banner => "banners",
            AssetType.Background => "backgrounds",
            AssetType.Cover => "covers",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
        
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
}

public enum AssetType {
    Avatar,
    Banner,
    Background,
    Cover
}