using fluxel.Models;
using fluxel.Services.API.Components.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace fluxel.Services.API.Tests;

public class DependencyStackTest
{
    protected ServiceCollection Services { get; private set; } = new();
    protected ServiceProvider ServiceProvider { get; private set; } = null!;
    protected DatabaseContext Database { get; private set; } = null!;

    [SetUp]
    public virtual void Setup()
    {
        Services.AssignSharedServices();
        Services.AddSingleton<AuthModuleManager>();
        Services.SetupDatabase<DatabaseContext>(c => c.UseInMemoryDatabase(Guid.NewGuid().ToString("N")));

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
