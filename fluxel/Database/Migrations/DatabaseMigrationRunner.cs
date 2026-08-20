using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Midori.Database;
using Midori.Logging;

namespace fluxel.Database.Migrations;

public class DatabaseMigrationRunner
{
    private readonly IDatabaseTable<MigrationCompletion> completions;
    private readonly IServiceProvider services;

    private readonly List<DatabaseMigration> migrations = [];

    public DatabaseMigrationRunner(IDatabaseProvider db, IServiceProvider services)
    {
        completions = db.GetTable<MigrationCompletion>("_migrations");
        this.services = services;

        migrations.Add(new Migration001SplitAuthFromUser());
    }

    public async Task ExecuteUpgrades()
    {
        var current = completions.Find(x => true).Select(x => x.Version)
                                 .DefaultIfEmpty(0).Max();

        foreach (var migration in migrations.Where(x => x.Version > current))
        {
            Logger.Log($"Performing migration {migration.GetType().Name}. ({migration.Version})");
            await migration.Perform(services);
            completions.Add(new MigrationCompletion { ID = migration.GetType().Name, Version = migration.Version });
        }
    }
}
