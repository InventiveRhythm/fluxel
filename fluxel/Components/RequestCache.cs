using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using fluxel.Database;
using fluxel.Models;
using fluxel.Models.Clubs;
using fluxel.Models.Groups;
using fluxel.Models.Maps;
using fluxel.Models.Users;

namespace fluxel.Components;

/// <summary>
/// In-memory cache for certain database models. Used on a per-request basis to speed up queries.
/// </summary>
public class RequestCache
{
    public CacheSection<long, Club> Clubs { get; }
    public CacheSection<string, Group> Groups { get; }
    public CacheSection<long, Map> Maps { get; }
    public CacheSection<long, MapSet> MapSets { get; }
    public CacheSection<long, User> Users { get; }

    public RequestCache(ClubManager clubs, GroupManager groups, MapManager maps, UserManager users)
    {
        Clubs = new CacheSection<long, Club>(
            this,
            (id, item) => item.ID == id,
            (a, b) => a.ID == b.ID,
            clubs.Get,
            () => clubs.All
        );

        Groups = new CacheSection<string, Group>(
            this,
            (id, item) => item.ID == id,
            (a, b) => a.ID == b.ID,
            groups.Get,
            () => groups.All
        );

        Maps = new CacheSection<long, Map>(
            this,
            (id, map) => map.ID == id,
            (mapA, mapB) => mapA.ID == mapB.ID,
            maps.GetMap,
            () => maps.AllMaps
        );

        MapSets = new CacheSection<long, MapSet>(
            this,
            (id, set) => set.ID == id,
            (setA, setB) => setA.ID == setB.ID,
            maps.GetSet,
            () => maps.AllSets
        );

        Users = new CacheSection<long, User>(
            this,
            (id, user) => user.ID == id,
            (userA, userB) => userA.ID == userB.ID,
            users.Get,
            () => users.AllUsers
        );
    }

    public class CacheSection<K, V>
    {
        private List<V> items { get; } = new();
        private bool hasAll;

        private RequestCache parent { get; }
        private Func<K, V, bool> matchFunc { get; }
        private Func<V, V, bool> compFunc { get; }
        private Func<K, V?> getFunc { get; }
        private Func<List<V>> allFunc { get; }

        public List<V> All
        {
            get
            {
                EnsureAll();
                return items;
            }
        }

        public CacheSection(RequestCache parent, Func<K, V, bool> matchFunc, Func<V, V, bool> compFunc, Func<K, V?> getFunc, Func<List<V>> allFunc)
        {
            this.parent = parent;
            this.matchFunc = matchFunc;
            this.getFunc = getFunc;
            this.allFunc = allFunc;
            this.compFunc = compFunc;
        }

        public void EnsureAll()
        {
            if (hasAll)
                return;

            var all = allFunc();

            foreach (var item in all)
            {
                applyCache(item);

                if (items.FirstOrDefault(x => compFunc(item, x)) is null)
                    items.Add(item);
            }

            hasAll = true;
        }

        public V? Get(K key)
        {
            var item = items.FirstOrDefault(x => matchFunc(key, x));

            if (item != null)
                return item;

            // we already have all,
            // do not bother asking the db again
            if (item is null && hasAll)
                return item; // can't just return 'null' here

            item = getFunc(key);

            if (item != null)
            {
                applyCache(item);
                items.Add(item);
            }

            return item;
        }

        public bool TryGet(K key, [NotNullWhen(true)] out V? item)
        {
            item = Get(key);
            return item != null;
        }

        private void applyCache(V item)
        {
            if (item is IHasCache c)
                c.Cache = parent;
        }
    }
}
