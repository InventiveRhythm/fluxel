using Midori.API.Attributes;
using Midori.API.Components;
using Midori.Utils;

namespace fluxel.Services.API.Controllers.Auth.OAuth;

[Controller("/.well-known")]
public class WellKnownController
{
    [HttpRoute("/openid-configuration")]
    [ReplyHandler(typeof(OAuthController.RawTextHandler))]
    public APIReturn<object> OpenIDConfig() => new
    {
        issuer = "http://host.docker.internal:2434",
        authorization_endpoint = "http://localhost:2434/oauth/authorize",
        token_endpoint = "http://host.docker.internal:2434/oauth/token",
        userinfo_endpoint = "http://host.docker.internal:2434/oauth/self",
        end_session_endpoint = "http://host.docker.internal:2434/oauth/logout",
        jwks_url = "http://host.docker.internal:2434/.well-known/jwk.json",
        response_types_supported = new[] { "code" },
        subject_types_supported = new[] { "public" },
        id_token_signing_alg_values_supported = new[] { "RS256" },
        scopes_supported = new[] { "openid", "profile", "email" },
    }.Serialize(true);
}
