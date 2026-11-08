/*
using System;
using System.Linq;
using fluxel.Models.Clubs;
using fluxel.Models.Notifications;
using fluxel.Models.Other;
using fluxel.Models.Users;
using Microsoft.EntityFrameworkCore;
using Midori.Utils;

namespace fluxel.Database;

[Obsolete]
public sealed class LegacyDatabaseContext : DbContext
{
    public DbSet<RewardedAchievement> Achievements { get; }
    public DbSet<Club> Clubs { get; }
    public DbSet<Notification> Notifications { get; }
    public DbSet<User> Users { get; }
    public DbSet<UserDiscordConnection> UserDiscordConnections { get; }
    public DbSet<UserStatistics> UserStatistics { get; }

    public LegacyDatabaseContext(DbContextOptions<LegacyDatabaseContext> opt)
        : base(opt)
    {
        Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;

        Achievements = Set<RewardedAchievement>();
        Clubs = Set<Club>();
        Notifications = Set<Notification>();
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

    public static bool HasAchievement(this LegacyDatabaseContext ctx, string id, long user)
        => ctx.Achievements.Count(x => x.AchievementID == id && x.UserID == user) > 0;

    #endregion
}
*/


