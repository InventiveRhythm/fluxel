using System;
using System.Net;
using System.Net.Http;
using fluxel.Config;
using fluxel.Database;
using fluxel.Models.Users;
using fluxel.Tasks;
using fluxel.Tasks.Users.Connections;
using fluxel.Utils.Steam;
using fluXis.Online.API.Models.Users.Connections;
using Midori.API.Attributes;
using Midori.API.Components;
using Midori.Logging;
using Midori.Utils;
using Newtonsoft.Json;
using HttpStatusCode = Midori.Networking.HttpStatusCode;
using WebRequest = osu.Framework.IO.Network.WebRequest;

namespace fluxel.API.Controllers.Users;

[Controller("/users/:id/connections")]
public class SingleUserConnectionsController
{
    private readonly UserManager users;
    private readonly DatabaseContext database;
    private readonly ServerConfig config;
    private readonly TaskRunner tasks;

    public SingleUserConnectionsController(UserManager users, ServerConfig config, DatabaseContext database, TaskRunner tasks)
    {
        this.users = users;
        this.config = config;
        this.database = database;
        this.tasks = tasks;
    }

    [Authenticated]
    [HttpRoute("/discord")]
    public APIReturn<string> GetDiscord(User auth, long id)
    {
        if (auth.ID != id)
            return Returns.Message(HttpStatusCode.Forbidden, "You cannot do this with another user.");

        var conn = database.UserDiscordConnections.Find(id);
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

            using (database.EditAndSave())
            {
                var existing = database.UserDiscordConnections.Find(id);
                if (existing != null) database.UserDiscordConnections.Remove(existing);

                database.UserDiscordConnections.Add(new UserDiscordConnection
                {
                    ID = id,
                    AccessToken = data.AccessToken,
                    RefreshToken = data.RefreshToken,
                    Expire = DateTimeOffset.Now.AddSeconds(data.ExpiresIn)
                });
            }

            tasks.Schedule(new LookupDiscordIDTask(id));
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
            var sid = SteamHelper.FetchUserIDFromTicket(config.Steam, token).Result;
            users.UpdateLocked(id, u => u.SteamID = sid);
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
