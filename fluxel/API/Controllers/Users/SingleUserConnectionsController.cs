using System;
using System.IO;
using System.Net.Http;
using fluxel.Components;
using fluxel.Config;
using fluxel.Database;
using fluxel.Models.Users;
using fluXis.Online.API.Models.Users.Connections;
using Midori.API.Attributes;
using Midori.API.Components;
using Midori.Logging;
using Midori.Networking;
using Midori.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using osu.Framework.IO.Network;

namespace fluxel.API.Controllers.Users;

[Controller("/users/:id/connections")]
public class SingleUserConnectionsController
{
    private readonly UserManager users;
    private readonly Donations donations;
    private readonly ServerConfig config;

    public SingleUserConnectionsController(UserManager users, ServerConfig config, Donations donations)
    {
        this.users = users;
        this.config = config;
        this.donations = donations;
    }

    [Authenticated]
    [HttpRoute("/discord")]
    public APIReturn<string> GetDiscord(User auth, long id)
    {
        if (auth.ID != id)
            return Returns.Message(HttpStatusCode.Forbidden, "You cannot do this with another user.");

        var conn = users.GetDiscord(id);
        if (conn is null || conn.Expire < DateTimeOffset.Now) return Returns.NotFound();

        return conn.AccessToken;
    }

    [Authenticated]
    [HttpRoute("/discord", APIMethod.Put)]
    public APIReturn<string> Discord(User auth, long id, [Source(ParameterSource.Body)] DiscordConnectionPayload payload)
    {
        if (auth.ID != id)
            return Returns.Message(HttpStatusCode.Forbidden, "You cannot connect accounts for other users.");

        var req = new WebRequest("https://discord.com/api/v10/oauth2/token");
        req.Method = HttpMethod.Post;
        req.AddParameter("grant_type", "authorization_code");
        req.AddParameter("client_id", config.Discord.ClientID);
        req.AddParameter("client_secret", config.Discord.ClientSecret);
        req.AddParameter("code", payload.Code);
        req.AddParameter("redirect_uri", payload.RedirectUri);
        req.AddParameter("code_verifier", payload.CodeVerifier);

        try
        {
            req.Perform();

            var data = req.GetResponseString().Deserialize<DiscordCodeResponse>()!;

            if (string.IsNullOrWhiteSpace(data.AccessToken) || string.IsNullOrWhiteSpace(data.RefreshToken))
                return Returns.Message(HttpStatusCode.BadRequest, "Failed to authorize.");

            users.AddOrUpdate(new UserDiscordConnection
            {
                ID = id,
                AccessToken = data.AccessToken,
                RefreshToken = data.RefreshToken,
                Expire = DateTimeOffset.Now.AddSeconds(data.ExpiresIn)
            });
            return data.AccessToken;
        }
        catch (Exception ex)
        {
            var res = req.GetResponseString();
            if (res != null) Logger.Error(ex, res);

            return Returns.Message(HttpStatusCode.BadRequest, "Failed to authorize.");
        }
    }

    [Authenticated]
    [HttpRoute("/steam", APIMethod.Put)]
    public APIReturn<ulong> Steam(User auth, long id, [Source(ParameterSource.Form)] string token)
    {
        if (auth.ID != id)
            return Returns.Message(HttpStatusCode.Forbidden, "You cannot connect accounts for other users.");

        var req = new WebRequest("https://partner.steam-api.com/ISteamUserAuth/AuthenticateUserTicket/v1/");
        req.AddParameter("key", config.Steam.WebKey);
        req.AddParameter("appid", config.Steam.AppID.ToString());
        req.AddParameter("ticket", token);
        // req.AddParameter("identity", "");

        req.Perform();

        var result = new StreamReader(req.ResponseStream).ReadToEnd();

        var json = result.Deserialize<JObject>()!;
        var response = json["response"]?.ToObject<JObject>() ?? throw new InvalidOperationException();
        var param = response["params"]?.ToObject<JObject>() ?? throw new InvalidOperationException();
        var steamid = param["steamid"]?.ToObject<ulong>() ?? throw new InvalidOperationException();

        users.UpdateLocked(id, u => u.SteamID = steamid);
        return steamid;
    }

    [Authenticated]
    [HttpRoute("/kofi", APIMethod.Put)]
    public APIReturn<object> KoFi(User auth, long id, [Source(ParameterSource.Form)] string token)
    {
        if (auth.ID != id)
            return Returns.Message(HttpStatusCode.Forbidden, "You cannot connect accounts for other users.");
        if (!string.IsNullOrWhiteSpace(auth.KoFiEmail))
            return Returns.Message(HttpStatusCode.BadRequest, "You have already linked a Ko-Fi email.");

        if (!donations.Connect(token, id, out var error))
            return Returns.Message(HttpStatusCode.BadRequest, error);

        donations.Update(auth.ID);
        return Returns.Okay();
    }

    public class DiscordCodeResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonProperty("token_type")]
        public string TokenType { get; set; } = string.Empty;

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonProperty("scope")]
        public string Score { get; set; } = string.Empty;
    }
}
