using System.Reflection;
using fluxel.Models.Users;
using Midori.API.Components;
using Midori.Networking;
using Midori.Networking.Middleware;

namespace fluxel.API.Components;

/// <summary>
/// Handles '@me' instead of UserIDs for routes starting with '/users'.
/// </summary>
public class FluxelAtMeMiddleware : IParameterMiddleware
{
    public APIReturn<object>? Handle(HttpServerContext ctx, object controller, MethodInfo method, IParameterMiddleware.Data data)
    {
        if (!ctx.Request.Target.StartsWith("/users"))
            return null;

        if (data.RawPath.TryGetValue("id", out var value))
        {
            if (value != "@me")
                return null;

            if (!data.Auth.TryGetValue("auth", out var auth) || auth is not User u)
                return Returns.Message(HttpStatusCode.Unauthorized, "You need to be authorized to use @me as a UserID.");

            data.RawPath["id"] = u.ID.ToString();
        }

        return null;
    }
}
