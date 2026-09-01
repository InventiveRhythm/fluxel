using System;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using fluxel.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Midori.Logging;
using Midori.Utils;
using Newtonsoft.Json;
using osu.Framework.IO.Network;

namespace fluxel.Tasks.Users.Connections;

public class LookupDiscordIDTask : IBasicTask
{
    public string Name => $"{nameof(LookupDiscordIDTask)}({id})";

    private long id { get; }

    public LookupDiscordIDTask(long id)
    {
        this.id = id;
    }

    public async Task Run(IServiceProvider services)
    {
        var database = services.GetRequiredService<DatabaseContext>();
        var users = services.GetRequiredService<UserManager>();

        var match = await database.UserDiscordConnections
                                  .IgnoreAutoIncludes()
                                  .Include(x => x.User)
                                  .FirstOrDefaultAsync(x => x.ID == id);

        if (match is null || match.User.DiscordID != null)
            return;

        var req = new WebRequest("https://discord.com/api/v10/oauth2/@me");
        req.AddHeader("Authorization", $"Bearer {match.AccessToken}");

        try
        {
            await req.PerformAsync();

            var result = req.GetResponseString();
            if (result is null) return;

            var info = result.Deserialize<DiscordAuthInfo>()!;
            users.UpdateLocked(match.ID, u => u.DiscordID = info.User.Id);
        }
        catch (Exception ex)
        {
            var res = req.GetResponseString();
            if (res != null) Logger.Error(ex, $"Failed to refresh discord token for {id}: {res}");
        }
    }

    private class DiscordAuthInfo
    {
        [JsonProperty("user")]
        public DiscordUser User { get; set; } = null!;
    }
}
