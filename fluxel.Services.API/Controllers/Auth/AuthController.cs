using System;
using fluxel.Components.IPC;
using fluxel.Config;
using fluxel.Models;
using fluxel.Models.Auth;
using fluxel.Models.Users;
using fluxel.Services.API.Components.Auth;
using fluxel.Utils.Steam;
using fluXis.Online.API.Payloads.Auth;
using fluXis.Online.API.Responses.Auth;
using Microsoft.Extensions.Options;
using Midori.API.Attributes;
using Midori.API.Components;
using Midori.Logging;
using Midori.Networking;

namespace fluxel.Services.API.Controllers.Auth;

[Controller("/auth")]
public class AuthController
{
    private readonly AuthModuleManager modules;
    private readonly DatabaseContext database;
    private readonly SteamConfig steam;
    private readonly Valkey valkey;

    public AuthController(DatabaseContext database, AuthModuleManager modules, IOptions<SteamConfig> steam, Valkey valkey)
    {
        this.database = database;
        this.modules = modules;
        this.steam = steam.Value;
        this.valkey = valkey;
    }

    [HttpRoute("/login", APIMethod.Post)]
    public APIReturn<LoginResponse> Login(HttpServerContext ctx, [Source(ParameterSource.Body)] LoginPayload payload)
    {
        if (!modules.TryFind(payload.Method, out var method))
            return Returns.Message(HttpStatusCode.BadRequest, "Unknown auth method.");

        if (!method.TryAuthenticate(ctx, payload, out var user, out var message))
            return Returns.Message(HttpStatusCode.BadRequest, message);

        const string client_ua = "fluXis";
        var ua = ctx.Request.Headers["User-Agent"] ?? "";

        AuthSession session;

        using (database.EditAndSave())
        {
            if (ua.Equals(client_ua, StringComparison.InvariantCultureIgnoreCase)) // logging in though the game client
            {
                var old = database.AuthSessions.FindFromGame(user.ID, false);
                if (old != null) database.AuthSessions.Remove(old);

                session = database.AuthSessions.Create(user.ID, ctx.RemoteIP.ToString(), ua).Result;
            }
            else // logging in through the website
            {
                session = database.AuthSessions.FindByIP(user.ID, ctx.RemoteIP.ToString(), true)
                          ?? database.AuthSessions.Create(user.ID, ctx.RemoteIP.ToString(), ua).Result;
            }
        }

        return new LoginResponse(session.Token, session.UserID);
    }

    [HttpRoute("/register", APIMethod.Post)]
    public APIReturn<RegisterResponse> Register(HttpServerContext ctx, [Source(ParameterSource.Body)] RegisterPayload payload)
    {
        var ip = ctx.RemoteIP.ToString();
        var ua = ctx.Request.Headers["User-Agent"] ?? string.Empty;
        if (ua != "fluXis") return Returns.Message(HttpStatusCode.BadRequest, "no.");

        // TODO: blacklist
        // if (payload.Username.IsBlacklisted())
        // return Returns.Message(HttpStatusCode.BadRequest, "This username is not available!");

        if (database.Users.ByUsername(payload.Username) != null)
            return Returns.Message(HttpStatusCode.BadRequest, "Username is already taken!");

        ulong sid;

        try
        {
            sid = SteamHelper.FetchUserIDFromTicket(steam, payload.SteamTicket).Result;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to pull SteamID!");
            return Returns.Message(HttpStatusCode.BadRequest, "Failed to get SteamID from ticket.");
        }

        if (database.Users.BySteamID(sid) != null)
            return Returns.Message(HttpStatusCode.BadRequest, "This steam account already has a user associated with it.");

        User user = database.Users.Add(new User
        {
            Username = payload.Username,
            SteamID = sid,
            Profile = new UserProfile()
        }).Entity;

        database.SaveChanges();
        var session = database.AuthSessions.Create(user.ID, ip, ua).Result;
        using (database.EditAndSave()) user.CountryCode = session.Country;

        // todo: add to channels and log register (in discord)
        _ = valkey.Publish(Valkey.UserCreate, new ValkeyIDEvent(user.ID));
        return new RegisterResponse(session.Token);
    }
}
