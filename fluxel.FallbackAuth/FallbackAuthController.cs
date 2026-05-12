using fluxel.Database;
using fluxel.Tasks;
using fluxel.Tasks.Logging;
using fluxel.Tasks.Users;
using fluxel.Utils;
using fluXis.Online.API.Payloads.Auth;
using fluXis.Online.API.Responses.Auth;
using fluXis.Utils;
using Midori.API.Attributes;
using Midori.API.Components;
using Midori.Networking;

namespace fluxel.FallbackAuth;

[Controller("/auth")]
public class FallbackAuthController
{
    private readonly UserManager users;
    private readonly TaskRunner tasks;

    public FallbackAuthController(UserManager users, TaskRunner tasks)
    {
        this.users = users;
        this.tasks = tasks;
    }

    [HttpRoute("/login", APIMethod.Post)]
    public APIReturn<LoginResponse> Login(HttpServerContext ctx, [Source(ParameterSource.Body)] LoginPayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload.Username) || string.IsNullOrWhiteSpace(payload.Password))
            return Returns.Message(HttpStatusCode.BadRequest, "Username and password must not be empty.");

        if (!users.TryGet(payload.Username, out var user))
            return Returns.Message(HttpStatusCode.Unauthorized, "No user with that username");

        var ua = ctx.Request.Headers["User-Agent"] ?? "";

        var session = users.GetSessionFromIP(user.ID, ctx.RemoteIP.ToString())
                      ?? users.CreateSession(user.ID, ctx.RemoteIP.ToString(), ua).Result;

        return new LoginResponse(session.Token, session.UserID);
    }

    [HttpRoute("/register", APIMethod.Post)]
    public APIReturn<RegisterResponse> Register(HttpServerContext ctx, [Source(ParameterSource.Body)] RegisterPayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload.Username) || string.IsNullOrWhiteSpace(payload.Password) || string.IsNullOrWhiteSpace(payload.Email))
            return Returns.Message(HttpStatusCode.BadRequest, "Username, password and email must not be empty.");

        if (payload.Username.Length is < 3 or > 16)
            return Returns.Message(HttpStatusCode.BadRequest, "Username must be between 3 and 16 characters!");

        if (!payload.Username.Matches(Validate.USERNAME))
            return Returns.Message(HttpStatusCode.BadRequest, "Username can only contain A-Z, a-z, 0-9 and _!");

        if (users.UsernameExists(payload.Username))
            return Returns.Message(HttpStatusCode.BadRequest, "Username is already taken!");

        var ip = ctx.RemoteIP.ToString();
        var country = IpUtils.GetCountryCode(ip).Result;
        var user = users.Add(payload.Username, payload.Email, payload.Password, country);

        tasks.Schedule(new LogUserRegistrationTask(user.ID));
        tasks.Schedule(new AddToDefaultChannelsTask(user.ID));

        var session = users.CreateSession(user.ID, ip, "fluXis").Result;
        return new RegisterResponse(session.Token);
    }
}
