using System.Collections.Generic;
using System.Linq;
using fluxel.Database.Extensions;
using fluxel.Models;
using fluxel.Models.Maps;
using fluxel.Models.Scores;
using Midori.Database;

namespace fluxel.Database;

public class ScoreManager
{
    public const string TABLE_NAME = "scores";

    private readonly IDatabaseTable<Score> scores;
    private readonly CounterManager counters;

    public List<Score> All => scores.Find(s => true).ToList();
    public long Count => scores.Count(u => true);

    public ScoreManager(IDatabaseProvider db, CounterManager counters)
    {
        this.counters = counters;
        scores = db.GetTable<Score>(TABLE_NAME);
    }

    public void Add(Score score)
    {
        score.ID = counters.GetNext(CounterType.Score);
        scores.Add(score);
    }

    public void DeleteAllFromMap(long map) => scores.DeleteMultiple(x => x.MapID == map);

    public Score? Get(long id) => scores.Find(u => u.ID == id).FirstOrDefault();
    public List<Score> GetByUser(long id) => scores.Find(u => u.UserID == id).ToList();
    public void Update(Score score) => scores.Replace(s => s.ID == score.ID, score);

    public List<Score> FromMap(Map map)
        => scores.Find(s => s.MapID == map.ID).ToList();

    public List<Score> FromMap(Map map, string? version)
        => FromMap(map).Where(s => s.MatchesVersion(version)).ToList();

    public Score? GetFirst(long mapId)
        => All.Where(s => s.MapID == mapId).MaxBy(s => s.PerformanceRating);
}
