using System;
using System.Threading.Tasks;

namespace fluxel.Database.Migrations;

public abstract class DatabaseMigration
{
    public abstract long Version { get; }
    public abstract Task Perform(IServiceProvider services);
}
