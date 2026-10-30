using System;

namespace fluxel.Models.Maps.Flags;

[Flags]
public enum MapIncludes
{
    FileName = 1 << 0,
    Claims = 1 << 1
}
