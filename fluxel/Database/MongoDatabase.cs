using MongoDB.Driver;

namespace fluxel.Database;

public static class MongoDatabase
{
    private static IMongoDatabase database = null!;

    public static void Setup()
    {
        var client = new MongoClient("mongodb://localhost:27017");
        database = client.GetDatabase("fluxel");
    }

    public static IMongoCollection<T> GetCollection<T>(string name) => database.GetCollection<T>(name);
}
