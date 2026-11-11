using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;

namespace fluxel.Services.Content.Extensions;

public static class ZipExtensions
{
    public static bool TryGetEntry(this ZipArchive archive, string path, [NotNullWhen(true)] out ZipArchiveEntry? entry)
    {
        entry = archive.GetEntry(path);
        return entry != null;
    }
}
