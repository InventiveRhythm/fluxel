using fluxel.Database.Helpers;
using fluxel.Models.Maps;
using fluxel.Models.Users;

namespace fluxel.Database.Extensions;

public static class MapSetExtensions
{
    public static User? GetCreator(this MapSet set) => set.Cache.Users.Get(set.CreatorID) ?? UserHelper.Get(set.CreatorID);

    public static bool AllowScores(this MapSet set) => set.Status >= MapStatus.Pure;
}
