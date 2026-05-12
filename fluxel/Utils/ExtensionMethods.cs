using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using osu.Framework.Graphics;

namespace fluxel.Utils;

#nullable disable

public static class ExtensionMethods
{
    public static async Task ForEachAsync<T>(this IEnumerable<T> tasks, Func<T, Task> func)
    {
        foreach (var task in tasks)
            await func(task);
    }

    public static long ToUnixSeconds(this DateTime dt)
    {
        var dto = new DateTimeOffset(dt);
        return dto.ToUnixTimeSeconds();
    }

    public static DiscordColor ToDiscord(this Colour4 color)
    {
        var str = color.ToHex();
        return new DiscordColor(str[..7]);
    }
}
