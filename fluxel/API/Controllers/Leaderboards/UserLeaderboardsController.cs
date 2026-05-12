using System.Collections.Generic;
using System.Linq;
using fluxel.Components;
using fluxel.Database;
using fluxel.Models.Users;
using fluXis.Online.API.Models.Users;
using Midori.API.Attributes;
using Midori.API.Components;

namespace fluxel.API.Controllers.Leaderboards;

[Controller("/leaderboards/users")]
public class UserLeaderboardsController
{
    private readonly UserManager users;
    private readonly MapManager maps;
    private readonly ModelTranslator translator;

    public UserLeaderboardsController(UserManager users, ModelTranslator translator, MapManager maps)
    {
        this.users = users;
        this.translator = translator;
        this.maps = maps;
    }

    [HttpRoute("/overall")]
    public APIReturn<List<APIUser>> OverallRating([Source(ParameterSource.Query)] int mode = 0)
    {
        if (mode is > 8 or < 4)
            mode = 0;

        return users.AllUsers.OrderByDescending(getRating).Take(100)
                    .Select(u => translator.ToAPI(u, mode: mode, include: UserIncludes.Statistics))
                    .Where(u => u.Statistics!.OverallRating > 0).ToList();

        double getRating(User u)
        {
            if (mode == 0)
                return u.OverallRating;

            var m = u.GetModeStatistics(mode);
            return m.OverallRating;
        }
    }

    [HttpRoute("/maps/uploaded")]
    public APIReturn<object> UploadedMaps()
    {
        var sets = maps.AllSets;
        var byUser = sets.GroupBy(x => x.CreatorID)
                         .OrderByDescending(x => x.Count()).Take(25);

        return byUser.Select(x =>
        {
            var user = translator.Cache.Users.Get(x.Key);

            return new
            {
                user = user != null ? translator.ToAPI(user) : APIUser.CreateUnknown(x.Key),
                maps = x.Select(s => translator.ToAPI(s)).ToList()
            };
        }).ToList();
    }

    [HttpRoute("/maps/pure")]
    public APIReturn<object> PureMaps()
    {
        var sets = maps.AllPureSets;
        var byUser = sets.GroupBy(x => x.CreatorID)
                         .OrderByDescending(x => x.Count()).Take(25);

        return byUser.Select(x =>
        {
            var user = translator.Cache.Users.Get(x.Key);

            return new
            {
                user = user != null ? translator.ToAPI(user) : APIUser.CreateUnknown(x.Key),
                maps = x.Select(s => translator.ToAPI(s)).ToList()
            };
        }).ToList();
    }
}
