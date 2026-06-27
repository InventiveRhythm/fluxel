using System;
using System.Net.Http;
using System.Threading.Tasks;
using fluxel.API.Controllers.Users;
using fluxel.Config;
using fluxel.Database;
using fluxel.Models.Users;
using Microsoft.Extensions.DependencyInjection;
using Midori.Logging;
using Midori.Utils;
using osu.Framework.IO.Network;

namespace fluxel.Tasks.Users.Connections;

public class RefreshDiscordTask : IBasicTask
{
    public string Name => $"{nameof(RefreshDiscordTask)}()";

    private long id { get; }

    public RefreshDiscordTask(long id)
    {
        this.id = id;
    }

    public Task Run(IServiceProvider services)
    {
        var config = services.GetRequiredService<ServerConfig>();
        var users = services.GetRequiredService<UserManager>();
        var match = users.GetDiscord(id);
        if (match is null) throw new InvalidOperationException();

        var req = new WebRequest("https://discord.com/api/v10/oauth2/token");
        req.Method = HttpMethod.Post;
        req.AddParameter("grant_type", "refresh_token");
        req.AddParameter("client_id", config.Discord.ClientID);
        req.AddParameter("client_secret", config.Discord.ClientSecret);
        req.AddParameter("refresh_token", match.RefreshToken);

        try
        {
            req.Perform();

            var data = req.GetResponseString().Deserialize<SingleUserConnectionsController.DiscordCodeResponse>()!;

            if (string.IsNullOrWhiteSpace(data.AccessToken) || string.IsNullOrWhiteSpace(data.RefreshToken))
                return Task.CompletedTask; // TODO: check error type

            users.AddOrUpdate(new UserDiscordConnection
            {
                ID = id,
                AccessToken = data.AccessToken,
                RefreshToken = data.RefreshToken,
                Expire = DateTimeOffset.Now.AddSeconds(data.ExpiresIn)
            });
        }
        catch (Exception ex)
        {
            var res = req.GetResponseString();
            if (res != null) Logger.Error(ex, $"Failed to refresh discord token for {id}: {res}");
        }

        return Task.CompletedTask;
    }
}
