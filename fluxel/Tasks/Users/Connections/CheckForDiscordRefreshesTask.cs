using System;
using System.Linq;
using System.Threading.Tasks;
using fluxel.Database;
using fluxel.Database.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using osu.Framework.Extensions.IEnumerableExtensions;

namespace fluxel.Tasks.Users.Connections;

public class CheckForDiscordRefreshesTask : IBasicTask
{
    public string Name => nameof(CheckForDiscordRefreshesTask);

    public Task Run(IServiceProvider services)
    {
        var tasks = services.GetRequiredService<TaskRunner>();
        var database = services.GetRequiredService<DatabaseContext>();

        database.UserDiscordConnections.GetExpiring()
                .ForEach(x => tasks.Schedule(new RefreshDiscordTask(x.ID)));

        database.UserDiscordConnections
                .IgnoreAutoIncludes()
                .Include(x => x.User)
                .Where(x => x.User.DiscordID == null).ToArray()
                .ForEach(x => tasks.Schedule(new LookupDiscordIDTask(x.ID)));

        return Task.CompletedTask;
    }
}
