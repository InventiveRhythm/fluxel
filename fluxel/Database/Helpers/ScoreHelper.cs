using System.Collections.Generic;
using System.Linq;
using fluxel.Database.Extensions;
using fluxel.Models;
using fluxel.Models.Maps;
using fluxel.Models.Scores;
using MongoDB.Driver;

namespace fluxel.Database.Helpers;

//TODO: right now when querying anything from here the Score field of the ScoreExtraPlayers won't be filled
//(but maybe that's not a big deal, since currently it is only useful when processing the score?)
public static class ScoreHelper
{
    private static IMongoCollection<Score> scores => MongoDatabase.GetCollection<Score>("scores");

    public static List<Score> All => scores.Find(s => true).ToList();
    public static long Count => scores.CountDocuments(u => true);

    public static void Add(Score score)
    {
        score.ID = CounterHelper.GetNext(CounterType.Score);
        scores.InsertOne(score);
    }

    public static void DeleteAllFromMap(long map) => scores.DeleteMany(x => x.MapID == map);

    public static Score? Get(long id) => scores.Find(u => u.ID == id).FirstOrDefault();
    public static List<Score> GetByUser(long id) => scores.Find(u => u.UserID == id).ToList();
    public static void Update(Score score) => scores.ReplaceOne(s => s.ID == score.ID, score);

    public static List<Score> FromMap(Map map)
        => scores.Find(s => s.MapID == map.ID).ToList();

    public static List<Score> FromMap(Map map, string? version)
        => FromMap(map).Where(s => s.MatchesVersion(version)).ToList();

    public static Score? GetFirst(long mapId)
        => All.Where(s => s.MapID == mapId).MaxBy(s => s.PerformanceRating);
}
