using osu.Framework.Platform;

namespace fluxel.Services.Content.Components;

public interface IStorageProvider
{
    Storage CreateStorage(string space, string path);
}
