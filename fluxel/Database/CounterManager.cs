using System;
using System.Linq;
using fluxel.Models;
using fluxel.Models.Clubs;
using fluxel.Models.Maps;
using fluxel.Models.Scores;
using fluxel.Models.Users;
using Midori.Database;

namespace fluxel.Database;

public class CounterManager
{
    private readonly IDatabaseTable<Counter> counters;
    private readonly object threadLock = new();

    public CounterManager(IDatabaseProvider db)
    {
        counters = db.GetTable<Counter>("counters");
        add(CounterType.Club, db.GetTable<Club>(ClubManager.TABLE_NAME));
        add(CounterType.Map, db.GetTable<Map>(MapManager.MAP_TABLE_NAME));
        add(CounterType.MapSet, db.GetTable<MapSet>(MapManager.MAPSET_TABLE_NAME));
        add(CounterType.Score, db.GetTable<Score>(ScoreManager.TABLE_NAME));
        add(CounterType.User, db.GetTable<User>(UserManager.TABLE_NAME));
    }

    private void add<T>(CounterType type, IDatabaseTable<T> table)
        where T : IHasID
    {
        lock (threadLock)
        {
            var counter = counters.Find(c => c.Type == type).FirstOrDefault();

            if (counter is not null)
                return;

            var results = table.Find(x => true).ToList();

            counter = new Counter
            {
                Type = type,
                Value = results.Count != 0 ? results.Max(x => x.ID) + 1 : 1
            };

            counters.Add(counter);
        }
    }

    public long GetNext(CounterType type)
    {
        lock (threadLock)
        {
            var counter = counters.Find(c => c.Type == type).FirstOrDefault();

            if (counter is null)
                throw new ArgumentException($"Counter {type} has not been initialized!");

            var num = counter.GetAndIncrease();
            counters.Replace(x => x.Type == type, counter);
            return num;
        }
    }
}
