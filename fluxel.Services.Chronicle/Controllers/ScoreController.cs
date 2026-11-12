using System;
using System.Collections.Generic;
using System.Linq;
using fluxel.Components;
using fluxel.Database.Extensions;
using fluxel.Models;
using fluxel.Models.Maps.Flags;
using fluxel.Models.Scores;
using fluxel.Models.Users;
using fluxel.Models.Users.Flags;
using fluxel.Services.Chronicle.Components;
using fluxel.Utils;
using fluXis.Modes;
using fluXis.Mods;
using fluXis.Online.API.Models.Users;
using fluXis.Online.API.Payloads.Scores;
using fluXis.Online.API.Responses.Maps;
using fluXis.Online.API.Responses.Scores;
using fluXis.Scoring.Enums;
using fluXis.Scoring.Processing;
using fluXis.Utils;
using Microsoft.EntityFrameworkCore;
using Midori.API.Attributes;
using Midori.API.Components;
using Midori.Networking;
using Midori.Utils;
using osu.Framework.Extensions.EnumExtensions;

namespace fluxel.Services.Chronicle.Controllers;

[Controller("/scores")]
public class ScoreController
{
    private readonly DatabaseContext database;
    private readonly ModelTranslator translator;
    private readonly ScoreTickets tickets;
    private readonly GameModeManager modes;

    public ScoreController(DatabaseContext database, ModelTranslator translator, ScoreTickets tickets, GameModeManager modes)
    {
        this.database = database;
        this.translator = translator;
        this.tickets = tickets;
        this.modes = modes;
    }

    [Authenticated, HttpRoute("/ticket", APIMethod.Post)]
    public APIReturn<string> Ticket(User auth, [Source(ParameterSource.Body)] ScoreTicketPayload payload)
    {
        if (auth.BanFlags.HasFlagFast(UserBanFlag.LeaderboardBan))
            return Returns.Message(HttpStatusCode.Forbidden, "You are banned from submitting scores.");

        if (payload.Mods is null || string.IsNullOrEmpty(payload.MapHash))
            return Returns.Message(HttpStatusCode.BadRequest, "");

        var map = database.Maps.Include(x => x.MapSet)
                          .FirstOrDefault(x => x.SHA256Hash == payload.MapHash);

        if (map == null)
            return Returns.Message(HttpStatusCode.NotFound, "This map is not uploaded to the server!");
        if (!string.IsNullOrEmpty(map.EventSHA256Hash) && map.EventSHA256Hash != payload.EffectHash)
            return Returns.Message(HttpStatusCode.NotFound, "Failed to validate map.");
        if (!string.IsNullOrEmpty(map.StoryboardSHA256Hash) && map.StoryboardSHA256Hash != payload.StoryboardHash)
            return Returns.Message(HttpStatusCode.NotFound, "Failed to validate map.");

        var mods = payload.Mods.Select(ModUtils.GetFromAcronym).ToList();
        if (mods.Any(x => x is null || !x.Rankable))
            return Returns.Message(HttpStatusCode.BadRequest, "Some mods can not be used to submit scores.");

        float duration = map.Length;

        if (mods.FirstOrDefault(x => x is RateMod) is RateMod r)
            duration /= r.Rate;

        var completion = DateTimeOffset.Now.AddMilliseconds(duration);
        var ticket = RandomizeUtils.GenerateRandomString(20);
        tickets.Tickets[ticket] = new ScoreTickets.Ticket(map.ID, completion);
        return ticket;
    }

    [Authenticated, HttpRoute("/", APIMethod.Post)]
    public APIReturn<ScoreSubmissionStats> Submit(User auth, [Source(ParameterSource.Body)] ScoreSubmissionPayload payload)
    {
        var submission = payload.Scores?.FirstOrDefault();

        if (submission is null || payload.Mods is null || payload.Replay is null || string.IsNullOrEmpty(payload.MapHash))
            return invalid();
        if (submission.UserID != auth.ID)
            return invalid();

        var map = database.Maps.Include(x => x.MapSet)
                          .FirstOrDefault(x => x.SHA256Hash == payload.MapHash);

        if (map == null)
            return Returns.Message(HttpStatusCode.NotFound, "This map is not uploaded to the server!");

        // var currentFirst =

        if (!tickets.Tickets.TryGetValue(payload.Ticket, out var ticket) || ticket.MapID != map.ID)
            return invalid();

        var delta = DateTimeOffset.Now - ticket.PredictedCompletion;
        if (delta > TimeSpan.FromSeconds(30)) return invalid();

        var mods = payload.Mods.Select(ModUtils.GetFromAcronym).ToArray();
        if (mods.Any(x => x is null || !x.Rankable)) return invalid();

        var mode = modes.Find(map.Mode);
        if (mode is null) return Returns.NotFound("gamemode");

        var max = map.HitObjectCounts.Sum(x => x.Value);
        if (max != submission.Results.Count) return invalid();

        var objects = submission.Results.GroupBy(x => x.Type)
                                .ToDictionary(x => x.Key, x => x.Count());

        if (map.HitObjectCounts.Any(counts => (counts.Value != 0 && objects.All(x => x.Key != counts.Key)) || counts.Value != objects[counts.Key]))
            return invalid();

        var judgements = submission.Results.GroupBy(x => x.Judgement)
                                   .ToDictionary(x => x.Key, x => x.Count());

        var score = new Score
        {
            MapID = map.ID,
            UserID = submission.UserID,
            MapHash = map.SHA256Hash,
            Mods = [.. mods.Select(x => x.Acronym)],
            ScrollSpeed = submission.ScrollSpeed,
            HasReplay = payload.Replay != null,
            HighestCombo = maxCombo(),
            FlawlessCount = judgements.GetValueOrDefault(Judgement.Flawless, 0),
            PerfectCount = judgements.GetValueOrDefault(Judgement.Perfect, 0),
            GreatCount = judgements.GetValueOrDefault(Judgement.Great, 0),
            AlrightCount = judgements.GetValueOrDefault(Judgement.Alright, 0),
            OkayCount = judgements.GetValueOrDefault(Judgement.Okay, 0),
            MissCount = judgements.GetValueOrDefault(Judgement.Miss, 0)
        };

        score.Accuracy = score.CalculateAccuracy();
        score.NumericScore = score.CalculateScore(map);
        score.PerformanceRating = ScoreProcessor.CalculatePerformance(
            (float)map.DifficultyRating,
            score.Accuracy,
            score.FlawlessCount,
            score.PerfectCount,
            score.GreatCount,
            score.AlrightCount,
            score.OkayCount,
            score.MissCount,
            [.. mods]
        );
        score.Rank = score.Accuracy switch
        {
            100 => ScoreRank.X,
            >= 99 => ScoreRank.SS,
            >= 98 => ScoreRank.S,
            >= 95 => ScoreRank.AA,
            >= 90 => ScoreRank.A,
            >= 80 => ScoreRank.B,
            >= 70 => ScoreRank.C,
            _ => ScoreRank.D
        };
        database.SaveChanges();

        // TODO: recalc

        return new ScoreSubmissionStats(
            translator.ToAPI(score),
            0, 0, 0,
            0, 0, 0
        );

        StatusReturn invalid() => Returns.Message(HttpStatusCode.BadRequest, "Failed to verify score.");

        int maxCombo()
        {
            var count = 0;
            var m = 0;

            foreach (var result in submission.Results)
            {
                count++;
                if (result.Judgement == Judgement.Miss) count = 0;
                m = Math.Max(m, count);
            }

            return m;
        }
    }

    [Authenticated(Required = false)]
    [HttpRoute("/maps/:id")]
    public APIReturn<MapLeaderboard> MapLeaderboard(
        User? auth, long id,
        [Source(ParameterSource.Query)] string type = "global",
        [Source(ParameterSource.Query)] string? version = null
    )
    {
        var map = database.Maps
                          .Include(x => x.MapSet)
                          .FirstOrDefault(x => x.ID == id);

        if (map is null)
            return Returns.NotFound("map");

        version ??= map.SHA256Hash;

        var query = database.Scores
                            .Include(x => x.User)
                            .Where(x => x.MapHash == version);

        switch (type)
        {
            case "global":
                break;

            case "country" when auth != null && string.IsNullOrWhiteSpace(auth.CountryCode):
                return Returns.Message(HttpStatusCode.BadRequest, "We don't know which country you are in. oT-To");

            case "country" when auth != null:
                query = query.Where(x => x.User!.CountryCode == auth.CountryCode);
                break;

            case "club" when auth is { ClubID: null }:
                return Returns.Message(HttpStatusCode.BadRequest, "You are not in a club.");

            case "club" when auth != null:
                query = query.Where(x => x.User!.ClubID == auth.ClubID);
                break;

            case "friends" when auth != null:
                var ids = database.UserFollows
                                  .Where(x => x.FollowerID == auth.ID)
                                  .ToArray().Select(x => x.FolloweeID);

                query = query.Where(x => ids.Contains(x.UserID));
                break;

            default:
                return Returns.Message(HttpStatusCode.BadRequest, $"Unknown leaderboard type '{type}'. (Are you logged in?)");
        }

        query = query.OrderByDescending(x => x.PerformanceRating)
                     .GroupBy(x => x.UserID)
                     .Select(g => g.OrderByDescending(x => x.PerformanceRating).First());

        return new MapLeaderboard(
            translator.ToAPI(map.MapSet!),
            translator.ToAPI(map),
            query.ToArray()
                 .OrderByDescending(x => x.PerformanceRating)
                 .Take(50)
                 .Select(x => translator.ToAPI(x))
        );
    }

    [HttpRoute("/users/:id")]
    public APIReturn<APIUserScores> User(long id)
    {
        if (!database.Users.TryFind(out var user, id))
            return Returns.NotFound("user");

        var all = database.Scores
                          .Include(x => x.Map).ThenInclude(x => x!.MapSet)
                          .Where(x => x.UserID == user.ID)
                          .Where(x => x.Map!.MapSet!.Status >= MapSetStatus.Pure)
                          .GroupBy(x => x.MapID)
                          .Select(g => g.OrderByDescending(x => x.PerformanceRating).First())
                          .ToArray();

        var recent = all.OrderByDescending(x => x.Time).Take(24);
        var best = all.OrderByDescending(x => x.PerformanceRating).Take(32);

        return new APIUserScores(
            [.. recent.Select(x => translator.ToAPI(x, ScoreIncludes.Map))],
            [.. best.Select(x => translator.ToAPI(x, ScoreIncludes.Map))]
        );
    }
}
