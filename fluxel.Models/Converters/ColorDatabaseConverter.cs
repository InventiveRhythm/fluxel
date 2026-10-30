using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using osu.Framework.Extensions.Color4Extensions;
using osuTK.Graphics;

namespace fluxel.Models.Converters;

public class ColorDatabaseConverter() : ValueConverter<Color4, string>(
    v => v.ToHex(),
    v => Color4Extensions.FromHex(v)
);
