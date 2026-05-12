using System.Collections.Generic;
using System.Linq;
using fluxel.Database;
using fluxel.Database.Extensions;
using Midori.API;
using Midori.Networking;

namespace fluxel.API;

public class FluxelAPIAuthenticator : IAPIAuthenticator
{
    private readonly UserManager users;

    public FluxelAPIAuthenticator(UserManager users)
    {
        this.users = users;
    }

    public bool Authenticate(HttpServerContext ctx, out List<string> scopes, out Dictionary<string, object> data)
    {
        scopes = new List<string>();
        data = new Dictionary<string, object>();

        var token = ctx.Request.Headers["Authorization"];

        if (string.IsNullOrEmpty(token))
            return false;

        token = token.Split(" ").Last().Trim();

        var session = users.GetSessionFromToken(token);

        if (session == null)
            return false;

        var user = users.Get(session.UserID);

        if (user == null)
            return false;

        if (user.IsDeveloper())
        {
            scopes.Add(Scopes.DEV);
            scopes.Add(Scopes.MOD);
            scopes.Add(Scopes.PURIFY);
        }
        else
        {
            if (user.IsModerator())
                scopes.Add(Scopes.MOD);
            if (user.IsPurifier())
                scopes.Add(Scopes.PURIFY);
        }

        data["auth"] = user;
        return true;
    }
}
