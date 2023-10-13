using fluxel.Components.Maps;
using MongoDB.Driver;

namespace fluxel.Database.Helpers;

public static class MapHelper
{
    private static IMongoCollection<Map> maps => MongoDatabase.GetCollection<Map>("maps");

    public static List<Map> All => maps.Find(m => true).ToList();
    public static long Count => maps.CountDocuments(m => true);

    public static long NextId {
        get
        {
            var map = maps.Find(m => true).SortByDescending(m => m.Id).FirstOrDefault();
            return map?.Id + 1 ?? 1;
        }
    }

    public static void Add(Map map) => maps.InsertOne(map);

    public static void Remove(Map map)
    {
        var filter = Builders<Map>.Filter.Eq(m => m.Id, map.Id);
        maps.DeleteOne(filter);
    }

    public static Map? Get(long id) => maps.Find(m => m.Id == id).FirstOrDefault();
    public static Map? GetByHash(string hash) => maps.Find(m => m.Hash == hash).FirstOrDefault();
}
