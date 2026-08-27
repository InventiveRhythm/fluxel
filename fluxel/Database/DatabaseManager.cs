using System;

namespace fluxel.Database;

public class DatabaseManager
{
    protected DatabaseContext Database { get; }

    public DatabaseManager(DatabaseContext database)
    {
        Database = database;
    }

    public IDisposable EditAndSave() => Database.EditAndSave();
}
