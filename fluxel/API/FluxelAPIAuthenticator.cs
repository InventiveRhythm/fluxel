using System.Collections.Generic;
using System.Linq;
using fluxel.Models;
using Midori.API;
using Midori.Networking;

namespace fluxel.API;

public class FluxelAPIAuthenticator : IAPIAuthenticator
{
    private readonly DatabaseContext database;

    public FluxelAPIAuthenticator(DatabaseContext database)
    {
        this.database = database;
    }

    public bool Authenticate(HttpServerContext ctx, out List<string> scopes, out Dictionary<string, object> data)
    {
        scopes = new List<string>();
        data = new Dictionary<string, object>();

        var token = ctx.Request.Headers["Authorization"];
        token ??= ctx.Request.Cookies["token"];

        if (string.IsNullOrEmpty(token))
            return false;

        token = token.Split(" ").Last().Trim();

        var session = database.AuthSessions.FirstOrDefault(x => x.Token == token);

        if (session == null)
            return false;

        var user = database.Users.Find(session.UserID);

        if (user == null)
            return false;

        var groups = database.GroupMembers.Where(x => x.UserID == user.ID).ToArray();
        scopes.AddRange(groups.Select(x => x.GroupID));

        if (scopes.Contains(Scopes.DEV))
        {
            scopes.Add(Scopes.MOD);
            scopes.Add(Scopes.PURIFY);
        }

        data["auth"] = user;
        return true;
    }
}
