using System;
using System.Linq;
using fluxel.Models;
using Microsoft.EntityFrameworkCore;

namespace fluxel.Database;

public class CounterManager : DatabaseManager
{
    private readonly object threadLock = new();

    public CounterManager(DatabaseContext ctx)
        : base(ctx)
    {
        // we are calling this check every time the manager is initialized.
        // potentially move this down to the increase method
        add(CounterType.Club, Database.Clubs);

        // TODO: add back
        // add(CounterType.Map, db.GetTable<Map>(MapManager.MAP_TABLE_NAME));
        // add(CounterType.MapSet, db.GetTable<MapSet>(MapManager.MAPSET_TABLE_NAME));
        // add(CounterType.Score, db.GetTable<Score>(ScoreManager.TABLE_NAME));
        // add(CounterType.User, db.GetTable<User>(UserManager.TABLE_NAME));
    }

    private void add<T>(CounterType type, DbSet<T> table)
        where T : class, IHasID
    {
        lock (threadLock)
        {
            var counter = Database.Counters.Find(type);

            if (counter is not null)
                return;

            var max = table.Max(x => x.ID);

            counter = new Counter
            {
                Type = type,
                Value = max + 1
            };

            Database.Counters.Add(counter);
            Database.SaveChanges();
        }
    }

    public long GetNext(CounterType type)
    {
        using (EditAndSave())
        {
            var counter = Database.Counters.Find(type) ?? throw new ArgumentException($"Counter {type} has not been initialized!");
            var num = counter.GetAndIncrease();
            return num;
        }
    }
}
