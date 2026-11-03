using fluxel.Models;
using fluxel.Models.Users;
using fluxel.Services.API.Components.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Midori.API.Components;
using Midori.Logging;
using Midori.Utils;

namespace fluxel.Services.API.Tests;

public class DependencyStackTest
{
    protected ServiceCollection Services { get; private set; } = new();
    protected ServiceProvider ServiceProvider { get; private set; } = null!;
    protected DatabaseContext Database { get; private set; } = null!;

    protected static void LogResult<T>(APIReturn<T> res)
    {
        Logger.Log($"{res.Status}: {res.Message}", LoggingTarget.Network);
        if (res.Result != null) Logger.Log(res.Result.Serialize(), LoggingTarget.Network);
    }

    protected User CreateUser(long id) => Database.Users.Add(new User { ID = id, Username = $"test_user_{id}" }).Entity;

    [SetUp]
    public virtual void Setup()
    {
        Services.AssignSharedServices();
        Services.AddSingleton<AuthModuleManager>();
        Services.SetupDatabase<DatabaseContext>(c =>
        {
            c.UseInMemoryDatabase(Guid.NewGuid().ToString("N"));
            c.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
        });

        ServiceProvider = Services.BuildServiceProvider();
        Database = ServiceProvider.GetRequiredService<DatabaseContext>();
    }

    [TearDown]
    public virtual void Teardown()
    {
        ServiceProvider.Dispose();
        Database.Dispose();
    }
}
