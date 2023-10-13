using fluxel.Components.Maps;
using fluxel.Components.Scores;
using MongoDB.Driver;

namespace fluxel.Database.Helpers;

public static class ScoreHelper
{
    private static IMongoCollection<Score> scores => MongoDatabase.GetCollection<Score>("scores");

    public static long Count => scores.CountDocuments(u => true);

    public static long NextId {
        get
        {
            var score = scores.Find(s => true).SortByDescending(s => s.Id).FirstOrDefault();
            return score?.Id + 1 ?? 1;
        }
    }

    public static void Add(Score score) => scores.InsertOne(score);
    public static Score? Get(long id) => scores.Find(u => u.Id == id).FirstOrDefault();
    public static List<Score> GetByUser(long id) => scores.Find(u => u.UserId == id).ToList();

    public static List<Score> FromMap(Map map) => scores.Find(s => s.MapId == map.Id).ToList();
}
