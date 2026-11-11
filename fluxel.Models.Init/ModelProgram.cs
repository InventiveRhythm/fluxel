using DotNetEnv;
using fluxel.Models.Groups;
using fluXis.Graphics.UserInterface.Color;
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
        if (File.Exists("docker/.env")) Env.Load("docker/.env");
        Env.Load();

        var build = new HostApplicationBuilder();
        build.Services.AddDbContext<DatabaseContext>(c => c.UseNpgsql(
            DatabaseContext.BuildConnectionString(x => Env.GetString(x)),
            n => n.ConfigureDataSource(d => d.EnableDynamicJson())
        ));

        var host = build.Build();

        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            db.Database.EnsureCreated(); // TODO: this should go after the migrations are created
            db.Database.MigrateAsync();
            // db.Database.EnsureDeleted();
        }

        // yes, this needs a separate scope
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

            if (!db.Groups.Any())
            {
                db.Groups.Add(new Group { ID = "dev", Name = "Developers", Tag = "DEV", Color = Theme.Footer1 });
                db.Groups.Add(new Group { ID = "mod", Name = "Moderator Team", Tag = "MT", Color = Theme.Footer2 });
                db.Groups.Add(new Group { ID = "purify", Name = "Purifiers", Tag = "PRF", Color = Theme.Footer3 });
                db.SaveChanges();
            }
        }
    }
}
