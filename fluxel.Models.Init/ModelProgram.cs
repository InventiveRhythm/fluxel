using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace fluxel.Models.Init;

// this class is purely for testing
// and creating the migrations
internal abstract class ModelProgram
{
    public static void Main()
    {
        var build = new HostApplicationBuilder();
        build.Services.AddDbContext<DatabaseContext>(c => c.UseNpgsql(
            DatabaseContext.BuildConnectionString(Environment.GetEnvironmentVariable),
            n => n.ConfigureDataSource(d => d.EnableDynamicJson())
        ));

        var host = build.Build();

        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            db.Database.EnsureCreated();
            db.Database.MigrateAsync();
            // db.Database.EnsureDeleted();
        }
    }
}
