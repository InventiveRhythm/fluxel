using System;
using System.IO;
using fluxel.Components;
using fluxel.Config;
using fluxel.Database;
using fluxel.Models.Users;
using Midori.API.Attributes;
using Midori.API.Components;
using Midori.Networking;
using Midori.Utils;
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
    [HttpRoute("/discord", APIMethod.Put)]
    public APIReturn<object> Discord(User auth, long id, [Source(ParameterSource.Form)] string token)
    {
        if (auth.ID != id)
            return Returns.Message(HttpStatusCode.Forbidden, "You cannot connect accounts for other users.");

        var req = new WebRequest("https://discord.com/api/users/@me");
        req.AddHeader("Authorization", $"Bearer {token}");
        req.Perform();

        var result = new StreamReader(req.ResponseStream).ReadToEnd();
        var data = result.Deserialize<JObject>()!;

        if (!data.ContainsKey("id"))
            return Returns.Message(HttpStatusCode.BadRequest, $"Failed fetch user.");

        ulong uid = ulong.Parse(data["id"]!.Value<string>()!);
        users.UpdateLocked(id, u => u.DiscordID = uid);
        return Returns.Okay();
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
}
