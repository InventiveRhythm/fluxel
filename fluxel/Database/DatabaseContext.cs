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
    public DbSet<UserDiscordConnection> UserDiscordConnections { get; }
    public DbSet<UserStatistics> UserStatistics { get; }

    public DatabaseContext(DbContextOptions<DatabaseContext> opt)
        : base(opt)
    {
        Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;

        Achievements = Set<RewardedAchievement>();
        Counters = Set<Counter>();
        Clubs = Set<Club>();
        Users = Set<User>();
        UserDiscordConnections = Set<UserDiscordConnection>();
        UserStatistics = Set<UserStatistics>();
    }

    protected override void OnModelCreating(ModelBuilder build)
    {
        base.OnModelCreating(build);

        // TODO: make consumers only request when needed
        build.Entity<User>(e => e.Navigation(x => x.Statistics).AutoInclude());
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
