using System.Globalization;
using DSharpPlus.Entities;
using osu.Framework.Extensions.Color4Extensions;
using osuTK.Graphics;

namespace fluxel.Services.Discord.Utils;

public static class ColorUtils
{
    public static DiscordColor ToDiscord(this Color4 color)
    {
        var str = color.ToHex();
        return new DiscordColor(str[..7]);
    }

    public static DiscordColor ParseHex(string hex)
    {
        hex = hex.Replace("#", "");

        switch (hex.Length)
        {
            case 3:
            {
                var r = byte.Parse(hex[0].ToString(), NumberStyles.HexNumber);
                var g = byte.Parse(hex[1].ToString(), NumberStyles.HexNumber);
                var b = byte.Parse(hex[2].ToString(), NumberStyles.HexNumber);
                return new DiscordColor(r, g, b);
            }

            case 6:
            {
                var r = byte.Parse(hex[..2], NumberStyles.HexNumber);
                var g = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
                var b = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
                return new DiscordColor(r, g, b);
            }

            default:
                return DiscordColor.White;
        }
    }
}
