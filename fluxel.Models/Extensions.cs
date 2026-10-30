using osu.Framework.Graphics;
using osuTK.Graphics;

namespace fluxel.Models;

public static partial class Extensions
{
    public static uint ToRGBA(this Color4 color)
    {
        Colour4 c = color;
        return c.ToRGBA();
    }
}
