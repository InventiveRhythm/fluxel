using System;
using System.ComponentModel;

namespace fluxel.Models.Maps.Flags;

[Flags]
public enum MapSetFlagsInternal : ulong
{
    [Description("Shadow-Ban (hide from search results)")]
    ShadowBan = 1 << 0
}
