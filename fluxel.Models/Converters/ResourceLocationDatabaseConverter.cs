using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using rhym;

namespace fluxel.Models.Converters;

public class ResourceLocationDatabaseConverter() : ValueConverter<ResourceLocation, string>(
    v => v.ToString(),
    v => ResourceLocation.FromString(v)
);
