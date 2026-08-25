using System;
using System.Threading.Tasks;
using fluxel.Database;
using Microsoft.Extensions.DependencyInjection;

namespace fluxel.Tasks.Users.Connections;

public class CheckForDiscordRefreshesTask : IBasicTask
{
    public string Name => nameof(CheckForDiscordRefreshesTask);

    public Task Run(IServiceProvider services)
    {
        var tasks = services.GetRequiredService<TaskRunner>();
        var users = services.GetRequiredService<UserManager>();
        var expiring = users.GetDiscordExpiring();
        expiring.ForEach(x => tasks.Schedule(new RefreshDiscordTask(x.ID)));
        return Task.CompletedTask;
    }
}
