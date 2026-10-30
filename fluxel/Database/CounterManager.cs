/*
using System.Linq;
using fluxel.Models;
using Microsoft.EntityFrameworkCore;

namespace fluxel.Database;

public class CounterManager : RelationalDatabaseManager
{
    private readonly object threadLock = new();

    public CounterManager(RelationalDatabaseContext ctx)
        : base(ctx)
    {
        // we are calling this check every time the manager is initialized.
        // potentially move this down to the increase method
        // add(CounterType.Club, Database.Clubs);

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
            var counter = Database.Counters.Find(type);
            counter ??= Database.Counters.Add(new Counter { Type = type, Value = getNextID(type) }).Entity;

            var num = counter.GetAndIncrease();
            return num;
        }
    }

    private long getNextID(CounterType type)
    {
        return type switch
        {
            // CounterType.Club => highest(Database.Clubs),
            // CounterType.Map => highest(Database.Map),
            // CounterType.MapSet => highest(Database.MapSets),
            // CounterType.Score => highest(Database.Scores),
            // CounterType.User => highest(Database.Users),
            _ => 1
        };

        long highest<T>(DbSet<T> set) where T : class, IHasID
        {
            var h = set.OrderByDescending(x => x.ID).FirstOrDefault();
            return h?.ID ?? 1;
        }
    }
}
*/
