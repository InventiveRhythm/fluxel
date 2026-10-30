/*
using System.Reflection;
using System.Text;
using System.Web;
using fluxel.Auth.Components;
using fluxel.Models.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Midori.API.Attributes;
using Midori.API.Components;
using Midori.API.Handlers;
using Midori.Networking;
using Midori.Utils;

namespace fluxel.Auth.API;

[Controller("/oauth")]
public class OAuthController
{
    private readonly OAuthManager oauth;
    private readonly UserManager users;

    public OAuthController(UserManager users, OAuthManager oauth)
    {
        this.users = users;
        this.oauth = oauth;
    }

    [Authenticated]
    [HttpRoute("/self")]
    [ReplyHandler(typeof(RawTextHandler))]
    public APIReturn<object> GetSelf(User auth) => new
    {
        id = auth.ID,
        name = auth.Username,
        fullname = auth.Username,
        email = $"notauser+{auth.ID}@flux.moe",
        email_verified = true,
        roles = auth.GroupIDs,
        backend = "oidc"
    }.Serialize(true);

    [Authenticated]
    [HttpRoute("/authorize")]
    [ReplyHandler(typeof(RedirectHandler))]
    public APIReturn<object> Authorize(
        User auth,
        // ReSharper disable InconsistentNaming
        [Source(ParameterSource.Query)] string client_id,
        [Source(ParameterSource.Query)] string redirect_uri,
        [Source(ParameterSource.Query)] string response_type,
        [Source(ParameterSource.Query)] string scope,
        [Source(ParameterSource.Query)] string state
        // ReSharper enable InconsistentNaming
    )
    {
        var app = oauth.FindApplication(client_id);
        if (app is null) return Returns.Message(HttpStatusCode.BadRequest, "Invalid client ID.");

        if (response_type != "code")
            return Returns.Message(HttpStatusCode.BadRequest, "'response_type' can only be 'code'.");

        if (!app.Redirects.Contains(redirect_uri))
            return Returns.Message(HttpStatusCode.BadRequest, "Invalid redirect URI.");

        var code = RandomizeUtils.GenerateRandomString(20);
        OpenID.Codes[code] = (auth.ID, client_id);
        return $"{redirect_uri}?code={code}&state={HttpUtility.UrlEncode(state)}";
    }

    [HttpRoute("/token", APIMethod.Post)]
    [ReplyHandler(typeof(RawTextHandler))]
    public APIReturn<string> Token(
        HttpServerContext ctx,
        [Source(ParameterSource.Form)] string code,
        [Source(ParameterSource.Form)] string redirect_uri,
        [Source(ParameterSource.Form)] string grant_type,
        [Source(ParameterSource.Form)] string client_id = "",
        [Source(ParameterSource.Form)] string client_secret = ""
    )
    {
        if (!OpenID.Codes.TryGetValue(code, out var resolved))
            return Returns.Message(HttpStatusCode.BadRequest, "Provided code is invalid.");

        if (string.IsNullOrWhiteSpace(client_id))
            client_id = resolved.client;

        var app = oauth.FindApplication(client_id);
        if (app is null) return Returns.Message(HttpStatusCode.BadRequest, "Invalid ClientID.");

        if (grant_type != "authorization_code")
            return Returns.Message(HttpStatusCode.BadRequest, "'grant_type' can only be 'authorization_code'.");

        if (client_secret != app.ClientSecret)
            return Returns.Message(HttpStatusCode.Unauthorized, "Client secret does not match.");
        if (!app.Redirects.Contains(redirect_uri))
            return Returns.Message(HttpStatusCode.BadRequest, "Invalid redirect URI.");

        OpenID.Codes.Remove(code);
        var user = users.Get(resolved.id)!;

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = "https://flux.moe",
            Audience = client_id,
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(OpenID.KEY, SecurityAlgorithms.RsaSha256),
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = user.ID.ToString(),
                [JwtRegisteredClaimNames.Name] = user.Username
            }
        };

        var handler = new JsonWebTokenHandler();
        var token = handler.CreateToken(descriptor);

        string ua = $"oauth-{client_id}";
        users.RemoveSessions(x => x.UserID == user.ID && x.UserAgent == ua);
        var session = users.CreateSession(user.ID, ctx.RemoteIP.ToString(), ua).Result;

        return new
        {
            access_token = session.Token,
            token_type = "Bearer",
            expires_in = 3600,
            id_token = token
        }.Serialize(true);
    }

    private class RedirectHandler : IAPIReplyHandler
    {
        public MethodInfo? TargetMethod { get; set; }
        private readonly HttpConfiguration config;

        public RedirectHandler(IOptions<HttpConfiguration> config) => this.config = config.Value;

        public void Handle<T>(HttpServerContext ctx, APIReturn<T> ret)
        {
            var res = new HttpResponse(ret.Status);
            config.ApplyHeaders(res.Headers);

            if (ret.Status == HttpStatusCode.OK)
            {
                res.StatusCode = HttpStatusCode.TemporaryRedirect;
                res.Headers["Location"] = ret.Result?.ToString();
            }
            else
            {
                var bytes = Encoding.UTF8.GetBytes(ret.Message);
                res.BodyStream.Write(bytes);
                res.ContentLength = bytes.Length;
            }

            res.WriteToStream(ctx.Stream).Wait();
        }
    }

    public class RawTextHandler : IAPIReplyHandler
    {
        public MethodInfo? TargetMethod { get; set; }
        private readonly HttpConfiguration config;

        public RawTextHandler(IOptions<HttpConfiguration> config) => this.config = config.Value;

        public void Handle<T>(HttpServerContext ctx, APIReturn<T> ret)
        {
            var res = new HttpResponse(ret.Status);
            config.ApplyHeaders(res.Headers);

            if (ret.Result is null)
            {
                var bytes = Encoding.UTF8.GetBytes(ret.Message);
                res.BodyStream.Write(bytes);
                res.ContentLength = bytes.Length;
            }
            else
            {
                if (ret.Result is not string str)
                    throw new InvalidCastException();

                var bytes = Encoding.UTF8.GetBytes(str);
                res.BodyStream.Write(bytes);
                res.ContentLength = bytes.Length;
            }

            res.WriteToStream(ctx.Stream).Wait();
        }
    }
}
*/
