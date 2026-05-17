using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using fluxel.Components;
using fluxel.Constants;
using fluxel.Database;
using fluxel.Database.Extensions;
using fluxel.Models.Maps;
using fluxel.Models.Scores;
using fluxel.Models.Users;
using fluXis.Online.API.Models.Maps;
using fluXis.Online.API.Responses.Maps;
using Midori.API.Attributes;
using Midori.API.Components;
using Midori.Networking;
using Newtonsoft.Json;

namespace fluxel.API.Controllers.Maps;

[Controller("/maps")]
public class MapsController
{
    private readonly MapManager maps;
    private readonly ScoreManager scores;
    private readonly UserManager users;
    private readonly ClubManager clubs;
    private readonly ModelTranslator translator;

    public MapsController(ModelTranslator translator, MapManager maps, ClubManager clubs, UserManager users, ScoreManager scores)
    {
        this.translator = translator;
        this.maps = maps;
        this.clubs = clubs;
        this.users = users;
        this.scores = scores;
    }

    [HttpRoute("/:id")]
    public APIReturn<APIMap> Get(long id)
    {
        var map = translator.Cache.Maps.Get(id);
        if (map == null) return Returns.NotFound();

        return translator.ToAPI(map);
    }

    [HttpRoute("/lookup")]
    public APIReturn<APIMapLookup> Lookup(
        [Source(ParameterSource.Query)] string hash = "",
        [Source(ParameterSource.Query)] int? mapper = null,
        [Source(ParameterSource.Query)] string title = "",
        [Source(ParameterSource.Query)] string artist = ""
    )
    {
        var map = maps.GetMap(m =>
            (string.IsNullOrEmpty(hash) || m.SHA256Hash == hash) &&
            (mapper == null || m.MapperIDs.Contains(mapper.Value)) &&
            (string.IsNullOrEmpty(title) || m.Title.Equals(title, StringComparison.CurrentCultureIgnoreCase)) &&
            (string.IsNullOrEmpty(artist) || m.Artist.Equals(artist, StringComparison.CurrentCultureIgnoreCase)));

        if (map == null)
            return Returns.Message(HttpStatusCode.NotFound, ResponseStrings.ProvidedTypeNotFound("map", "filters"));

        var set = maps.GetSet(map.SetID);

        if (set == null) // bail out if the map set is not found
            throw new InvalidOperationException("Attempted to get a deleted MapSet of a Map.");

        var mapLookup = new APIMapLookup
        {
            ID = map.ID,
            SetID = map.SetID,
            CreatorID = set.CreatorID,
            Rating = map.Rating,
            Status = (int)set.Status,
            DateSubmitted = set.Submitted.ToUnixTimeSeconds(),
            DateRanked = set.DateRanked?.ToUnixTimeSeconds(),
            LastUpdated = set.LastUpdated.ToUnixTimeSeconds(),
            Hash = map.SHA256Hash
        };

        return mapLookup;
    }

    [Authenticated]
    [HttpRoute("/hashes", APIMethod.Post)]
    public APIReturn<List<string>> GetHashes([Source(ParameterSource.Body)] List<long> ids)
    {
        translator.Cache.Maps.EnsureAll();

        return ids.Select(x =>
        {
            var map = translator.Cache.Maps.Get(x);
            return map is null ? string.Empty : $"{map.SHA256Hash}|{map.EffectSHA256Hash}|{map.StoryboardSHA256Hash}";
        }).ToList();
    }

    #region Rate Vote

    [Authenticated]
    [HttpRoute("/:id/rate", APIMethod.Post)]
    public APIReturn<double> SubmitRateVote(User auth, long id, [Source(ParameterSource.Body)] RateVotePayload payload)
    {
        var map = maps.GetMap(id);

        if (map == null)
            return Returns.NotFound("map");
        if (map.MapperIDs.Contains(auth.ID) && !auth.IsDeveloper())
            return Returns.Message(HttpStatusCode.BadRequest, ResponseStrings.CannotRateOwnMap);
        if (maps.HasRateVoted(auth.ID, id))
            return Returns.Message(HttpStatusCode.BadRequest, ResponseStrings.AlreadyVoted);

        var purifier = auth.IsPurifier();

        maps.AddRateVote(auth.ID, id, payload.Base!.Value, payload.Reading!.Value, payload.Tracking!.Value, payload.Perception!.Value, purifier);

        map.RecalculateRating(maps);
        maps.Update(map);

        return map.Rating;
    }

    [Authenticated(Scopes.DEV)]
    [HttpRoute("/:id/refresh-rate")]
    public APIReturn<double> RefreshRating(long id)
    {
        var map = translator.Cache.Maps.Get(id);
        if (map == null) return Returns.NotFound();

        var rating = map.RecalculateRating(maps);
        maps.QuickUpdate(map.ID, m => m.Rating = rating);
        return rating;
    }

    public class RateVotePayload
    {
        [JsonProperty("base")]
        [Required, Range(0, 20)]
        public float? Base { get; set; }

        [JsonProperty("read")]
        [Required, Range(0, 5)]
        public float? Reading { get; set; }

        [JsonProperty("track")]
        [Required, Range(0, 5)]
        public float? Tracking { get; set; }

        [JsonProperty("percept")]
        [Required, Range(0, 5)]
        public float? Perception { get; set; }
    }

    #endregion

    #region Leaderboards

    [Authenticated]
    [HttpRoute("/:id/scores")]
    public APIReturn<MapLeaderboard> Leaderboard(
        User auth, long id,
        [Source(ParameterSource.Query)] string type = "global",
        [Source(ParameterSource.Query)] string? version = null
    )
    {
        var map = maps.GetMap(id);
        if (map is null) return Returns.NotFound("map");

        var set = maps.GetSet(map.SetID);
        if (set is null) return Returns.NotFound("mapset");

        version ??= map.SHA256Hash;

        switch (type)
        {
            case "global":
            {
                var all = scores.FromMap(map, version).ToList();
                return replyLeaderboard(auth, set, map, filterLeaderboardList(all.OrderByDescending(s => s.PerformanceRating).ToList()));
            }

            case "country":
                if (string.IsNullOrEmpty(auth?.CountryCode))
                    return Returns.Message(HttpStatusCode.BadRequest, "We don't know which country you are in. oT-To");

                return replyLeaderboard(auth, set, map, getLeaderboardCountry(map, version, auth.CountryCode));

            case "club":
                if (!clubs.TryGetWhereUserIsMember(auth, out var club))
                    return Returns.Message(HttpStatusCode.BadRequest, "You are not in a club.");

                return replyLeaderboard(auth, set, map, getLeaderboardClub(map, version, club.ID));

            case "friends":
            {
                var following = users.GetFollowing(auth.ID);
                following.Add(auth.ID);

                var all = scores.FromMap(map, version).Where(s => following.Contains(s.UserID)).ToList();
                return replyLeaderboard(auth, set, map, filterLeaderboardList(all.OrderByDescending(s => s.PerformanceRating).ToList()));
            }

            // case "clubs":
            //     return new MapLeaderboardClubs(translator.ToAPI(map), clubs.GetScoresOnMap(map.ID).OrderByDescending(s => s.PerformanceRating).Select(s => translator.ToAPI(s)));
        }

        return Returns.Message(HttpStatusCode.BadRequest, "The parameter 'type' is not valid. Valid types are 'global', 'country', 'club' or 'friends'.");
    }

    private MapLeaderboard replyLeaderboard(User user, MapSet set, Map map, IEnumerable<Score> sc)
    {
        return new MapLeaderboard(translator.ToAPI(set), translator.ToAPI(map), sc.Select(s =>
        {
            var api = translator.ToAPI(s);
            api.User.Following = users.GetFollowState(user.ID, api.User.ID);
            return api;
        }));
    }

    private static List<Score> filterLeaderboardList(List<Score> all)
    {
        var sc = new List<Score>();

        foreach (var score in all)
        {
            if (sc.Count >= 50)
                break;

            if (sc.Any(s => s.UserID == score.UserID))
                continue;

            sc.Add(score);
        }

        return sc;
    }

    private List<Score> getLeaderboardCountry(Map map, string? version, string code)
        => filterLeaderboardList(scores.FromMap(map, version)
                                       .Where(s => users.Get(s.UserID)?.CountryCode == code)
                                       .OrderByDescending(s => s.PerformanceRating).ToList());

    private List<Score> getLeaderboardClub(Map map, string? version, long id)
    {
        return filterLeaderboardList(scores.FromMap(map, version)
                                           .Where(s => users.TryGet(s.UserID, out var u) && clubs.GetWhereUserIsMember(u)?.ID == id)
                                           .OrderByDescending(s => s.PerformanceRating)
                                           .ToList());
    }

    #endregion
}
