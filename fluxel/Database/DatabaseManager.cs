using System;

namespace fluxel.Database;

public class DatabaseManager
{
    protected DatabaseContext Database { get; }

    protected DatabaseManager(DatabaseContext database)
    {
        Database = database;
    }

    public IDisposable EditAndSave() => Database.EditAndSave();
}
