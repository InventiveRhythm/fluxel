/*
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using fluxel.Components;
using fluxel.Database;
using fluXis.Online.API.Models.Maps;
using Newtonsoft.Json;

namespace fluxel.Models.Users;

[Table(UserManager.TABLE_NAME)]
public class LegacyUser
{
    public int GetGlobalRank(RequestCache cache, int mode = 0) => getRank(mode, cache);
    public int GetCountryRank(RequestCache cache, int mode = 0) => getRank(mode, cache, e => e.Where(u => u.CountryCode == ""));

    private int getRank(int mode, RequestCache cache, Func<IEnumerable<User>, IEnumerable<User>>? func = null)
    {
        return 0;
        /*if (OverallRating == 0)
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
        }#1#
    }

    public void Recalculate(ScoreManager sm, MapManager maps, RequestCache cache)
    {
        /*var scores = sm.GetByUser(ID);
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

            var stat = Statistics.FirstOrDefault(x => x.Mode == mode.ToString());
            var wasNull = stat is null;
            stat ??= new UserStatistics { UserID = ID, Mode = mode.ToString() };
            stat.OverallRating = UserExtensions.CalculateOverallRating(best);
            stat.PotentialRating = UserExtensions.CalculatePotentialRating(best, recent);
            if (wasNull) Statistics.Add(stat);
        }#1#
    }

    /*public UserStatistics GetModeStatistics(int mode)
    {
        if (mode is < 4 or > 8)
            throw new ArgumentOutOfRangeException(nameof(mode));

        var str = $"{mode}";
        var m = Statistics.FirstOrDefault(x => x.Mode == str);
        return m ?? new UserStatistics();
    }#1#

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
            /*this.user = user;

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
            LimitMaximumCount = mm.GetUploadLimit(user);#1#
        }
    }
}
*/
