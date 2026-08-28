using System;
using System.Linq;
using fluxel.Models;
using fluxel.Models.Clubs;
using fluxel.Models.Other;
using fluxel.Models.Users;
using Microsoft.EntityFrameworkCore;
using Midori.Utils;

namespace fluxel.Database;

public sealed class DatabaseContext : DbContext
{
    public DbSet<RewardedAchievement> Achievements { get; }
    public DbSet<Counter> Counters { get; }
    public DbSet<Club> Clubs { get; }
    public DbSet<User> Users { get; }
    public DbSet<UserStatistics> UserStatistics { get; }

    public DatabaseContext(DbContextOptions<DatabaseContext> opt)
        : base(opt)
    {
        Achievements = Set<RewardedAchievement>();
        Counters = Set<Counter>();
        Clubs = Set<Club>();
        Users = Set<User>();
        UserStatistics = Set<UserStatistics>();
    }

    public IDisposable EditAndSave() => new InvokeOnDisposal(() => SaveChanges());
}

public static class DatabaseContextExtensions
{
    #region Achievements

    public static bool HasAchievement(this DatabaseContext ctx, string id, long user)
        => ctx.Achievements.Count(x => x.AchievementID == id && x.UserID == user) > 0;

    #endregion
}
