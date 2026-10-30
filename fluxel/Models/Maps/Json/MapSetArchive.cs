using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using fluXis.Map;
using osu.Framework.Extensions.IEnumerableExtensions;

namespace fluxel.Models.Maps.Json;

public class MapSetArchive : IDisposable
{
    public ZipArchive Archive { get; }

    public Dictionary<string, PlayableMap> Maps { get; } = new();
    public Dictionary<string, Stream> Other { get; } = new();

    public MapSetArchive(ZipArchive archive)
    {
        Archive = archive;
    }

    public void Dispose()
    {
        Archive.Dispose();
        Other.ForEach(o => o.Value.Dispose());
        GC.SuppressFinalize(this);
    }
}
