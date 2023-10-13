using fluxel.Components.Maps;
using MongoDB.Driver;

namespace fluxel.Database.Helpers;

public static class MapSetHelper
{
    private static IMongoCollection<MapSet> sets => MongoDatabase.GetCollection<MapSet>("mapsets");

    public static List<MapSet> All => sets.Find(m => true).ToList();
    public static long Count => sets.CountDocuments(m => true);

    public static void Add(MapSet set) => sets.InsertOne(set);

    public static MapSet? Get(long id) => sets.Find(m => m.Id == id).FirstOrDefault();
    public static IEnumerable<MapSet> GetByCreator(long id) => sets.Find(m => m.CreatorId == id).ToList();
}
