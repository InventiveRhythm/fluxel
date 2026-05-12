using System.Linq;
using fluxel.Models.Collections;
using Midori.Database;
using MongoDB.Bson;

namespace fluxel.Database;

public class CollectionManager
{
    private readonly IDatabaseTable<Collection> collections;

    public CollectionManager(IDatabaseProvider db)
    {
        collections = db.GetTable<Collection>("collections");
    }

    public void Add(Collection col) => collections.Add(col);

    public Collection? Get(string id) => !ObjectId.TryParse(id, out var obj) ? null : Get(obj);
    public Collection? Get(ObjectId id) => collections.Find(x => x.ID == id).FirstOrDefault();
}
