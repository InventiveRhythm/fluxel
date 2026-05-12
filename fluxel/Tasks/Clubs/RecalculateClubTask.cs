using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using fluxel.Database;
using fluxel.Models.Users;
using Microsoft.Extensions.DependencyInjection;

namespace fluxel.Tasks.Clubs;

public class RecalculateClubTask : IBasicTask
{
    public string Name => $"RecalculateClub({id})";

    private long id { get; }

    public RecalculateClubTask(long id)
    {
        this.id = id;
    }

    public Task Run(IServiceProvider services)
    {
        var clubs = services.GetRequiredService<ClubManager>();
        var club = clubs.Get(id);

        if (club == null)
            throw new ArgumentException($"No club with id {id} was found!");

        var members = club.GetMemberList(services.GetRequiredService<UserManager>());
        var scores = clubs.GetScores(club.ID);

        club.OverallRating = overall(members);
        club.TotalScore = scores.Sum(s => s.TotalScore);
        clubs.Update(club);
        return Task.CompletedTask;
    }

    private static double overall(List<User> members)
    {
        var ovr = 0d;
        var count = 0;

        foreach (var member in members)
        {
            ovr += member.OverallRating * Math.Pow(.9f, count);
            count++;
        }

        return Math.Round(ovr, 2);
    }
}
