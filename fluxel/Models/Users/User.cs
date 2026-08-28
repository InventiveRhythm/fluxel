using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using fluxel.Components;
using fluxel.Database;
using fluxel.Database.Extensions;
using fluxel.Models.Clubs;
using fluxel.Models.Groups;
using fluxel.Models.Maps;
using fluXis.Online.API.Models.Maps;
using Newtonsoft.Json;

namespace fluxel.Models.Users;

[Table(UserManager.TABLE_NAME)]
public class User : IHasID
{
    [Key, Column("_id")]
    public long ID { get; init; }

    [Column("discord-id")]
    public ulong? DiscordID { get; set; }

    [Column("steam-id")]
    public ulong? SteamID { get; set; }

    [Column("name"), MaxLength(16)]
    public string Username { get; set; } = "";

    [Column("last-name-change")]
    public long LastNameChange { get; set; }

    [Column("force-name-change")]
    public bool ForceNameChange { get; set; }

    [Column("last-notification-read")]
    public long LastNotificationRead { get; set; }

    [Column("nick"), MaxLength(24)]
    public string? DisplayName { get; set; } = "";

    [Column("avatar"), MaxLength(64)]
    public string? AvatarHash { get; set; } = "";

    [Column("avatar_animated")]
    public bool? HasAnimatedAvatar { get; set; }

    [Column("banner"), MaxLength(64)]
    public string? BannerHash { get; set; } = "";

    [Column("banner_animated")]
    public bool? HasAnimatedBanner { get; set; }

    [Column("kofi-email"), MaxLength(64)]
    public string? KoFiEmail { get; set; }

    [Column("support-end")]
    public DateTime? SupportEndTime { get; set; }

    [Column("aboutme"), MaxLength(256)]
    public string AboutMe { get; set; } = "";

    [Column("pronouns"), MaxLength(16)]
    public string Pronouns { get; set; } = "";

    [Column("paint"), MaxLength(32)]
    public string Paint { get; set; } = "";

    [Column("groups")]
    public List<string> GroupIDs { get; set; } = new();

    [Column("club")]
    public long? ClubID { get; set; }

    [Column("role")]
    public int RoleInt { get; set; }

    [Column("ban-flags")]
    public UserBanFlag BanFlags { get; set; }

    [Column("country"), MaxLength(2)]
    public string? CountryCode { get; set; } = string.Empty;

    [Column("created")]
    public long CreatedAt { get; init; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    [Column("lastlogin")]
    public long LastLogin { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    [Column("ovr")]
    public double OverallRating { get; set; }

    [Column("ptr")]
    public double PotentialRating { get; set; }

    [Column("combo")]
    public int MaxCombo { get; set; }

    [Column("score")]
    public long RankedScore { get; set; }

    [Column("ova")]
    public double OverallAccuracy { get; set; }

    public List<UserStatistics> ModeStatistics { get; set; } = new();

    [NotMapped]
    public bool IsSupporter => SupportEndTime > DateTime.UtcNow || this.IsPurifier() || this.IsModerator();

    public List<Group> GetGroups(GroupManager gm)
        => [.. GroupIDs.Select(gm.Get).OfType<Group>()];

    public Club? GetClub(ClubManager cm) => ClubID == null ? null : cm.Get(ClubID.Value);

    public int GetGlobalRank(RequestCache cache, int mode = 0) => getRank(mode, cache);
    public int GetCountryRank(RequestCache cache, int mode = 0) => getRank(mode, cache, e => e.Where(u => u.CountryCode == CountryCode));

    private int getRank(int mode, RequestCache cache, Func<IEnumerable<User>, IEnumerable<User>>? func = null)
    {
        if (OverallRating == 0)
            return 0;

        var rank = 0;

        var users = cache.Users.All.AsEnumerable();

        if (func is not null)
            users = func(users);

        users = users.OrderByDescending(getUserModeOverallRating);

        foreach (var user in users)
        {
            rank++;
            if (user.ID == ID) break;
        }

        return rank;

        double getUserModeOverallRating(User user)
        {
            if (mode == 0)
                return user.OverallRating;

            var m = user.GetModeStatistics(mode);
            return m.OverallRating;
        }
    }

    public void Recalculate(ScoreManager sm, MapManager maps, RequestCache cache)
    {
        var scores = sm.GetByUser(ID);
        var best = this.GetBestScores(cache, scores);
        var recent = this.GetRecentScores(cache, scores);

        OverallRating = UserExtensions.CalculateOverallRating(best);
        PotentialRating = UserExtensions.CalculatePotentialRating(best, recent);
        MaxCombo = this.CalculateMaxCombo(maps, scores);
        RankedScore = this.CalculateRankedScore(maps, scores);
        OverallAccuracy = this.CalculateAccuracy(cache, scores);

        int[] modes = { 4, 5, 6, 7, 8 };

        foreach (var mode in modes)
        {
            best = this.GetBestScores(cache, scores, mode);
            recent = this.GetRecentScores(cache, scores, mode);

            var stat = ModeStatistics.FirstOrDefault(x => x.Mode == mode.ToString());
            var wasNull = stat is null;
            stat ??= new UserStatistics { UserID = ID, Mode = mode.ToString() };
            stat.OverallRating = UserExtensions.CalculateOverallRating(best);
            stat.PotentialRating = UserExtensions.CalculatePotentialRating(best, recent);
            if (wasNull) ModeStatistics.Add(stat);
        }
    }

    public UserStatistics GetModeStatistics(int mode)
    {
        if (mode is < 4 or > 8)
            throw new ArgumentOutOfRangeException(nameof(mode));

        var str = $"{mode}";
        var m = ModeStatistics.FirstOrDefault(x => x.Mode == str);
        return m ?? new UserStatistics();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class UserMaps
    {
        private User user { get; }

        [JsonProperty("ranked")]
        public IEnumerable<APIMapSet> Ranked { get; }

        [JsonProperty("unranked")]
        public IEnumerable<APIMapSet> Unranked { get; }

        [JsonProperty("guest_diffs")]
        public IEnumerable<APIMapSet> GuestDiffs { get; }

        [JsonProperty("limit_uploaded")]
        public long? LimitUploadedCount { get; }

        [JsonProperty("limit_max")]
        public long? LimitMaximumCount { get; }

        public UserMaps(MapManager mm, ModelTranslator translator, User user, User? requestedBy)
        {
            this.user = user;

            var byUser = mm.GetSetsByCreator(user.ID);
            byUser.Reverse();

            Ranked = byUser.Where(s => s.Status >= MapStatus.Pure).Select(x => translator.ToAPI(x, userid: requestedBy?.ID));
            Unranked = byUser.Where(s => s.Status < MapStatus.Pure).Select(x => translator.ToAPI(x, userid: requestedBy?.ID));

            var maps = mm.GetByMapsByMapper(user.ID);
            maps.Reverse();
            maps.RemoveAll(map => byUser.Any(s => s.ID == map.SetID));

            var ids = maps.Select(m => m.SetID).Distinct();
            GuestDiffs = ids.Select(mm.GetSet).OfType<MapSet>().Select(x => translator.ToAPI(x, userid: requestedBy?.ID));

            if (user.ID != requestedBy?.ID)
                return;

            LimitUploadedCount = mm.CountUploaded(user.ID, mm.UploadLimitStartDate);
            LimitMaximumCount = mm.GetUploadLimit(user);
        }
    }
}

[Flags]
public enum UserIncludes
{
    CreatedAt = 1 << 0,
    LastLogin = 1 << 1,
    Statistics = 1 << 2,
    Following = 1 << 3,
    Flags = 1 << 4
}

[Flags]
public enum UserExclude
{
    Club = 1 << 0
}
