using System;
using System.Collections.Generic;
using System.Linq;
using fluxel.Components;
using fluxel.Database.Extensions;
using fluxel.Models.Users;
using fluXis.Online.API.Models.Users;
using fluXis.Online.API.Responses.Users;
using Midori.API.Attributes;
using Midori.API.Components;
using Midori.Utils.Extensions;

namespace fluxel.API.Controllers.Users;

[Controller("/users")]
public class UsersController
{
    private readonly ModelTranslator translator;
    private readonly RequestCache cache;
    private readonly Statistics stats;

    public UsersController(RequestCache cache, ModelTranslator translator, Statistics stats)
    {
        this.cache = cache;
        this.translator = translator;
        this.stats = stats;
    }

    [Authenticated(Required = false)]
    [HttpRoute("/")]
    public APIReturn<List<APIUser>> List(
        User? auth,
        [Source(ParameterSource.Query)] int limit = 50,
        [Source(ParameterSource.Query)] int offset = 0,
        [Source(ParameterSource.Query)] string name = "",
        [Source(ParameterSource.Query)] string with = "")
    {
        limit = Math.Clamp(limit, 1, 100);

        var all = cache.Users.All;
        UserIncludes include = 0;

        foreach (var se in with.Split(","))
        {
            switch (se)
            {
                case "creation":
                    include |= UserIncludes.CreatedAt;
                    break;

                case "login":
                    include |= UserIncludes.LastLogin;
                    break;

                case "flags" when auth != null && auth.IsModerator():
                    include |= UserIncludes.Flags;
                    break;
            }
        }

        var users = all.Skip(offset).Where(n => string.IsNullOrWhiteSpace(name) || n.Username.ContainsLower(name) || (n.DisplayName?.ContainsLower(name) ?? false)).Take(limit)
                       .Select(x => translator.ToAPI(x, include: include));

        // TODO: pagination
        // interaction.SetPaginationInfo(limit, offset, all.Count, users.Count());
        return users.ToList();
    }

    [HttpRoute("/online")]
    public APIReturn<OnlineUsers> Online()
    {
        var users = new List<APIUser>();

        foreach (var uid in stats.OnlineUsers)
        {
            var user = cache.Users.Get(uid);
            users.Add(user != null ? translator.ToAPI(user) : APIUser.CreateUnknown(uid));
        }

        return new OnlineUsers(users.Count(x => x.ID != 0), users);
    }
}
