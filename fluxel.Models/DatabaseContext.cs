using System;
using System.Collections.Generic;
using System.Linq;
using fluxel.Models.Auth;
using fluxel.Models.Chat;
using fluxel.Models.Clubs;
using fluxel.Models.Collections;
using fluxel.Models.Converters;
using fluxel.Models.Cosmetics;
using fluxel.Models.Featured;
using fluxel.Models.Groups;
using fluxel.Models.Maps;
using fluxel.Models.Modding;
using fluxel.Models.Monetize;
using fluxel.Models.Notifications;
using fluxel.Models.Scores;
using fluxel.Models.Users;
using fluxel.Models.Users.Connections;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Midori.Utils;
using osuTK.Graphics;
using rhym;
using CollectionEntry = fluxel.Models.Collections.CollectionEntry;

namespace fluxel.Models;

public class DatabaseContext : DbContext
{
    public DbSet<AuthInfo> AuthInfos { get; }
    public DbSet<AuthSession> AuthSessions { get; }
    public DbSet<OAuthApplication> OAuthApps { get; }

    public DbSet<ChatChannel> ChatChannels { get; }
    public DbSet<ChatChannelMember> ChatChannelMembers { get; }
    public DbSet<ChatMessage> ChatMessages { get; }

    public DbSet<Club> Clubs { get; }
    public DbSet<ClubClaim> ClubClaims { get; }
    public DbSet<ClubInvite> ClubInvites { get; }
    public DbSet<ClubScore> ClubScores { get; }
    public DbSet<ClubStatistics> ClubStatistics { get; }

    public DbSet<Collection> Collections { get; }
    public DbSet<CollectionEntry> CollectionEntries { get; }

    public DbSet<NamePaint> NamePaints { get; }

    public DbSet<FeaturedArtist> FeaturedArtists { get; }
    public DbSet<FeaturedAlbum> FeaturedAlbums { get; }
    public DbSet<FeaturedTrack> FeaturedTracks { get; }

    public DbSet<Group> Groups { get; }
    public DbSet<GroupMember> GroupMembers { get; }

    public DbSet<Map> Maps { get; }
    public DbSet<MapUserState> MapUserStates { get; }
    public DbSet<MapSet> MapSets { get; }
    public DbSet<MapSetUserState> MapSetUserStates { get; }

    public DbSet<ModdingAction> ModdingActions { get; }

    public DbSet<KoFiPayment> KoFiPayments { get; }
    public DbSet<RedemptionCode> RedemptionCodes { get; }

    public DbSet<Notification> Notifications { get; }

    public DbSet<Score> Scores { get; }

    public DbSet<User> Users { get; }
    public DbSet<UserAchievement> UserAchievements { get; }
    public DbSet<UserFollow> UserFollows { get; }
    public DbSet<UserProfile> UserProfiles { get; }
    public DbSet<UserStatistics> UserStatistics { get; }
    public DbSet<UserDiscord> UserDiscords { get; }

    public DatabaseContext(DbContextOptions<DatabaseContext> opt)
        : base(opt)
    {
        AuthInfos = Set<AuthInfo>();
        AuthSessions = Set<AuthSession>();
        OAuthApps = Set<OAuthApplication>();

        ChatChannels = Set<ChatChannel>();
        ChatChannelMembers = Set<ChatChannelMember>();
        ChatMessages = Set<ChatMessage>();

        Clubs = Set<Club>();
        ClubClaims = Set<ClubClaim>();
        ClubInvites = Set<ClubInvite>();
        ClubScores = Set<ClubScore>();
        ClubStatistics = Set<ClubStatistics>();

        Collections = Set<Collection>();
        CollectionEntries = Set<CollectionEntry>();

        NamePaints = Set<NamePaint>();

        FeaturedArtists = Set<FeaturedArtist>();
        FeaturedAlbums = Set<FeaturedAlbum>();
        FeaturedTracks = Set<FeaturedTrack>();

        Groups = Set<Group>();
        GroupMembers = Set<GroupMember>();

        Maps = Set<Map>();
        MapUserStates = Set<MapUserState>();
        MapSets = Set<MapSet>();
        MapSetUserStates = Set<MapSetUserState>();

        ModdingActions = Set<ModdingAction>();

        KoFiPayments = Set<KoFiPayment>();
        RedemptionCodes = Set<RedemptionCode>();

        Notifications = Set<Notification>();

        Scores = Set<Score>();

        Users = Set<User>();
        UserAchievements = Set<UserAchievement>();
        UserFollows = Set<UserFollow>();
        UserProfiles = Set<UserProfile>();
        UserStatistics = Set<UserStatistics>();
        UserDiscords = Set<UserDiscord>();
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        base.ConfigureConventions(builder);

        builder.Properties<Color4>().HaveConversion<ColorDatabaseConverter>();
        builder.Properties<ResourceLocation>().HaveConversion<ResourceLocationDatabaseConverter>();
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AuthInfo>(c =>
        {
            c.HasDiscriminator<string>("method")
             .HasValue<LegacyAuthInfo>("legacy");

            c.HasKey(nameof(AuthInfo.UserID), "method");
        });

        builder.Entity<RedemptionCode>(c =>
        {
            c.HasDiscriminator<string>("type")
             .HasValue<DonatorRedemptionCode>("donate");
        });

        builder.Entity<Notification>(c =>
        {
            c.HasDiscriminator<string>("type")
             .HasValue<ClubInviteNotification>("club_invite")
             .HasValue<QueueNotification>("queue");
        });

        builder.Entity<Map>(c =>
        {
            c.Property<Dictionary<ResourceLocation, long>>(x => x.HitObjectCounts)
             .HasColumnName("objects").HasColumnType("jsonb")
             .HasConversion(
                 v => v.Serialize(),
                 v => v.Deserialize<Dictionary<ResourceLocation, long>>() ?? new Dictionary<ResourceLocation, long>()
             )
             .Metadata.SetValueComparer(new ValueComparer<Dictionary<ResourceLocation, long>>(
                 (c1, c2) => c1 != null && c2 != null && Enumerable.SequenceEqual(c1.OrderBy(kvp => kvp.Key.ToString()), c2.OrderBy(kvp => kvp.Key.ToString())),
                 d => d.Aggregate(0, (a, v) => HashCode.Combine(a, v.Key.GetHashCode(), v.Value.GetHashCode())),
                 d => d.ToDictionary(entry => entry.Key, entry => entry.Value)
             ));
        });
    }

    public IDisposable EditAndSave() => new InvokeOnDisposal(() => SaveChanges());

    public static string BuildConnectionString(Func<string, string?> getEnv)
    {
        var conn = $"Host={env("DATABASE_HOST", "localhost")};";
        conn += $"Port={env("DATABASE_PORT", "5432")};";
        conn += $"Database={env("DATABASE_DATABASE", "fluxel")};";
        conn += $"Username={env("DATABASE_USERNAME", "fluxel")};";
        conn += $"Password={env("DATABASE_PASSWORD", "")};";
        return conn;

        string env(string key, string fallback)
        {
            var v = getEnv(key);
            return string.IsNullOrWhiteSpace(v) ? fallback : v;
        }
    }
}
