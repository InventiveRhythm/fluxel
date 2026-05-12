using System;
using System.Collections.Generic;
using System.Linq;
using fluxel.Models.Other;
using Midori.Database;

namespace fluxel.Database;

public class EventManager
{
    private readonly IDatabaseTable<StoredEvent> collection;

    public EventManager(IDatabaseProvider db)
    {
        collection = db.GetTable<StoredEvent>("events");
    }

    public void Add(StoredEvent ev) => collection.Add(ev);

    public List<StoredEvent> GetActive()
    {
        var now = DateTimeOffset.Now.ToUnixTimeSeconds();
        var events = collection.Find(x => x.StartTime <= now && x.EndTime > now);
        return events.ToList();
    }
}
