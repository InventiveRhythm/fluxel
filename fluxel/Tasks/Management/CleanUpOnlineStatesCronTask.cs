using System;
using System.Linq;
using System.Threading.Tasks;
using fluxel.Database;
using Microsoft.Extensions.DependencyInjection;

namespace fluxel.Tasks.Management;

public class CleanupOnlineStatesCronTask : IBasicTask
{
    public string Name => "CleanUpOnlineStates";

    public Task Run(IServiceProvider services)
    {
        var users = services.GetRequiredService<UserManager>();

        var current = DateTimeOffset.Now.ToUnixTimeSeconds();
        const int duration = 2 * 24 * 60 * 60;

        foreach (var login in users.AllLogins.Where(login => login.Time < current - duration))
            users.ClearLogin(login);

        return Task.CompletedTask;
    }
}
