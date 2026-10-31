using System;
using System.Net;
using System.Net.Http;
using fluxel.Config;
using fluxel.Models;
using fluxel.Models.Users;
using fluxel.Models.Users.Connections;
using fluxel.Utils.Steam;
using fluXis.Online.API.Models.Users.Connections;
using Microsoft.Extensions.Options;
using Midori.API.Attributes;
using Midori.API.Components;
using Midori.Logging;
using Midori.Utils;
using Newtonsoft.Json;
using HttpStatusCode = Midori.Networking.HttpStatusCode;
using WebRequest = osu.Framework.IO.Network.WebRequest;

namespace fluxel.Services.API.Controllers.Users;

[Controller("/users/:id/connections")]
public class SingleUserConnectionsController
{
    private readonly DatabaseContext database;
    private readonly DiscordConfig discord;
    private readonly SteamConfig steam;

    public SingleUserConnectionsController(IOptions<DiscordConfig> discord, IOptions<SteamConfig> steam, DatabaseContext database)
    {
        this.discord = discord.Value;
        this.steam = steam.Value;
        this.database = database;
    }

    [Authenticated]
    [HttpRoute("/discord")]
    public APIReturn<string> GetDiscord(User auth, long id)
    {
        if (auth.ID != id)
            return Returns.Message(HttpStatusCode.Forbidden, "You cannot do this with another user.");

        var conn = database.UserDiscords.Find(id);
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
        req.AddParameter("client_id", discord.ClientID.ToString());
        req.AddParameter("client_secret", discord.ClientSecret);
        req.AddParameter("code", payload.Code);
        req.AddParameter("redirect_uri", payload.RedirectUri);
        req.AddParameter("code_verifier", payload.CodeVerifier);

        try
        {
            req.Perform();

            var data = req.GetResponseString().Deserialize<DiscordCodeResponse>()!;

            if (string.IsNullOrWhiteSpace(data.AccessToken) || string.IsNullOrWhiteSpace(data.RefreshToken))
                return Returns.Message(HttpStatusCode.BadRequest, "Failed to authorize.");

            using (database.EditAndSave())
            {
                var existing = database.UserDiscords.Find(id);
                if (existing != null) database.UserDiscords.Remove(existing);

                database.UserDiscords.Add(new UserDiscord
                {
                    UserID = id,
                    AccessToken = data.AccessToken,
                    RefreshToken = data.RefreshToken,
                    Expire = DateTimeOffset.Now.AddSeconds(data.ExpiresIn).UtcDateTime
                });
            }

            // TODO: notify discord service
            // tasks.Schedule(new LookupDiscordIDTask(id));
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

        try
        {
            var sid = SteamHelper.FetchUserIDFromTicket(steam, token).Result;

            using (database.EditAndSave())
                auth.SteamID = sid;

            return sid;
        }
        catch (WebException wex)
        {
            Logger.Error(wex, $"Failed to pull SteamID for user {id}!");
            return Returns.Message(HttpStatusCode.BadRequest, "Failed to get SteamID from ticket.");
        }
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
