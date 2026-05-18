using System;
using System.Collections.Generic;
using System.Linq;
using fluxel.Components;
using fluxel.Database;
using fluxel.Database.Extensions;
using fluxel.Models.Clubs;
using fluxel.Models.Groups;
using fluxel.Models.Maps;
using fluXis.Online.API.Models.Maps;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace fluxel.Models.Users;

public class User : IHasID
{
    [BsonId]
    public long ID { get; init; }

    [BsonElement("password")]
    public string Password { get; set; } = "";

    [BsonElement("totp-enabled")]
    public bool HasTOTP { get; set; }

    [BsonElement("discord-id")]
    public ulong? DiscordID { get; set; }

    [BsonElement("steam-id")]
    public ulong? SteamID { get; set; }

    [BsonIgnore]
    public bool HasMfa => HasTOTP;

    [BsonElement("name")]
    public string Username { get; set; } = "";

    [BsonElement("last-name-change")]
    public long LastNameChange { get; set; }

    [BsonElement("force-name-change")]
    public bool ForceNameChange { get; set; }

    [BsonElement("last-notification-read")]
    public long LastNotificationRead { get; set; }

    [BsonElement("nick")]
    public string? DisplayName { get; set; } = "";

    [BsonElement("avatar")]
    public string? AvatarHash { get; set; } = "";

    [BsonElement("avatar_animated")]
    public bool HasAnimatedAvatar { get; set; }

    [BsonElement("banner")]
    public string? BannerHash { get; set; } = "";

    [BsonElement("banner_animated")]
    public bool HasAnimatedBanner { get; set; }

    [BsonElement("email")]
    public string Email { get; init; } = "";

    [BsonElement("kofi-email")]
    public string? KoFiEmail { get; set; }

    [BsonElement("support-end")]
    public DateTime? SupportEndTime { get; set; }

    [BsonElement("aboutme")]
    public string AboutMe { get; set; } = "";

    [BsonElement("pronouns")]
    public string Pronouns { get; set; } = "";

    [BsonElement("paint")]
    public string Paint { get; set; } = "";

    [BsonElement("groups")]
    public List<string> GroupIDs { get; set; } = new();

    [BsonElement("role")]
    public int RoleInt { get; set; }

    [BsonElement("ban-flags")]
    public UserBanFlag BanFlags { get; set; }

    [BsonElement("country")]
    public string? CountryCode { get; set; } = string.Empty;

    [BsonElement("social")]
    public UserSocials Socials { get; init; } = new();

    [BsonElement("created")]
    public long CreatedAt { get; init; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    [BsonElement("lastlogin")]
    public long LastLogin { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    [BsonElement("modes")]
    public Dictionary<string, UserStatistics> ModeStatistics { get; set; } = new();

    [BsonElement("ovr")]
    public double OverallRating { get; set; }

    [BsonElement("ptr")]
    public double PotentialRating { get; set; }

    [BsonElement("combo")]
    public int MaxCombo { get; set; }

    [BsonElement("score")]
    public long RankedScore { get; set; }

    [BsonElement("ova")]
    public double OverallAccuracy { get; set; }

    [BsonIgnore]
    public bool IsSupporter => SupportEndTime > DateTime.UtcNow || this.IsPurifier() || this.IsModerator();

    public List<Group> GetGroups(GroupManager gm)
        => GroupIDs.Select(gm.Get).OfType<Group>().ToList();

    public Club? GetClub(ClubManager cm) => cm.GetWhereUserIsMember(this);

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

            var stat = GetModeStatistics(mode);
            stat.OverallRating = UserExtensions.CalculateOverallRating(best);
            stat.PotentialRating = UserExtensions.CalculatePotentialRating(best, recent);
        }
    }

    public UserStatistics GetModeStatistics(int mode)
    {
        if (mode is < 4 or > 8)
            throw new ArgumentOutOfRangeException(nameof(mode));

        var str = $"{mode}";

        if (!ModeStatistics.ContainsKey(str))
            ModeStatistics.Add(str, new UserStatistics());

        return ModeStatistics[str];
    }

    public class UserStatistics
    {
        [BsonElement("ovr")]
        public double OverallRating { get; set; }

        [BsonElement("ptr")]
        public double PotentialRating { get; set; }
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

            Ranked = byUser.Where(s => s.Status >= MapStatus.Pure).Select(x => translator.ToAPI(x));
            Unranked = byUser.Where(s => s.Status < MapStatus.Pure).Select(x => translator.ToAPI(x));

            var maps = mm.GetByMapsByMapper(user.ID);
            maps.Reverse();
            maps.RemoveAll(map => byUser.Any(s => s.ID == map.SetID));

            var ids = maps.Select(m => m.SetID).Distinct();
            GuestDiffs = ids.Select(mm.GetSet).OfType<MapSet>().Select(x => translator.ToAPI(x));

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
    Socials = 1 << 2,
    Statistics = 1 << 3,
    Following = 1 << 4,
    Email = 1 << 5,
    Flags = 1 << 6
}

[Flags]
public enum UserExclude
{
    Club = 1 << 0
}
