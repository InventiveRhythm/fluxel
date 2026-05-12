using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fluxel.Components;
using fluxel.Database;
using fluxel.Models.Scores;
using Microsoft.Extensions.DependencyInjection;

namespace fluxel.Tasks.Scores;

public class RecalculateClubScoreTask : IBasicTask
{
    public string Name => $"RecalculateClubScore(map={mapID}, club={clubID})";

    private long mapID { get; }
    private long clubID { get; }

    public RecalculateClubScoreTask(long mapID, long clubID)
    {
        this.mapID = mapID;
        this.clubID = clubID;
    }

    public Task Run(IServiceProvider services)
    {
        var clubs = services.GetRequiredService<ClubManager>();
        var map = services.GetRequiredService<MapManager>().GetMap(mapID);

        if (map == null)
            throw new ArgumentException($"No map with id {mapID} was found!");

        var scores = services.GetRequiredService<ScoreManager>().FromMap(map, map.SHA256Hash);
        var cache = services.GetRequiredService<RequestCache>();

        scores = scores.Where(s => cache.Users.TryGet(s.UserID, out var u) && clubs.GetWhereUserIsMember(u.ID)?.ID == clubID).ToList();
        scores = scores.OrderByDescending(s => s.TotalScore).ToList();

        // only take one score per user
        var uniqueScores = new List<Score>();

        foreach (var score in scores)
        {
            if (uniqueScores.Any(s => s.UserID == score.UserID))
                continue;

            uniqueScores.Add(score);
        }

        scores = uniqueScores;

        var clubScore = clubs.GetScore(clubID, mapID, true)!;
        var idx = 0;

        // reset stats
        clubScore.TotalScore = 0;
        clubScore.PerformanceRating = 0;
        clubScore.Accuracy = 0;

        foreach (var score in scores)
        {
            clubScore.TotalScore += score.TotalScore;
            clubScore.PerformanceRating += score.PerformanceRating * Math.Pow(.9f, idx);
            clubScore.Accuracy += score.Accuracy;
            idx++;
        }

        clubScore.Accuracy /= scores.Count; // average accuracy
        clubs.UpdateScore(clubScore);
        return Task.CompletedTask;
    }
}
