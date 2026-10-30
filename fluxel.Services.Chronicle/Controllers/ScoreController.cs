/*
using System.Text;
using fluxel.Components;
using fluxel.Components.Tasks;
using fluxel.Constants.Achievements;
using fluxel.Models.Scores;
using fluxel.Models.Users;
using fluXis.Mods;
using fluXis.Online.API.Payloads.Scores;
using fluXis.Online.API.Responses.Scores;
using fluXis.Scoring;
using fluXis.Scoring.Enums;
using fluXis.Utils;
using Midori.API.Attributes;
using Midori.API.Components;
using Midori.Logging;
using Midori.Networking;
using Midori.Utils;
using ScoreManager = fluxel.Database.ScoreManager;

namespace fluxel.ScoreSubmission;

[Controller("/scores")]
public class ScoreController
{
    private readonly ClubManager clubs;
    private readonly MapManager maps;
    private readonly ScoreManager scores;
    private readonly UserManager users;

    private ScoreSubmissionModule module;
    private readonly RequestCache cache;
    private readonly TaskRunner tasks;
    private readonly ModelTranslator translator;
    private readonly ServerEvents events;

    public ScoreController(MapManager maps, ScoreManager scores, ClubManager clubs, TaskRunner tasks, RequestCache cache, UserManager users, ModelTranslator translator, ServerEvents events,
                           ScoreSubmissionModule module)
    {
        this.maps = maps;
        this.scores = scores;
        this.clubs = clubs;
        this.tasks = tasks;
        this.cache = cache;
        this.users = users;
        this.translator = translator;
        this.events = events;
        this.module = module;
    }

    [Authenticated, HttpRoute("/ticket", APIMethod.Post)]
    public APIReturn<string> Ticket(User auth, [Source(ParameterSource.Body)] ScoreTicketPayload payload)
    {
        if (auth.HasFlag(UserBanFlag.LeaderboardBan))
            return Returns.Message(HttpStatusCode.Forbidden, "You are banned from submitting scores.");

        if (payload.Mods is null || string.IsNullOrEmpty(payload.MapHash))
            return Returns.Message(HttpStatusCode.BadRequest, "");

        var map = maps.GetMapByHash(payload.MapHash);

        if (map == null)
            return Returns.Message(HttpStatusCode.NotFound, "This map is not uploaded to the server!");
        if (!string.IsNullOrEmpty(map.EffectSHA256Hash) && map.EffectSHA256Hash != payload.EffectHash)
            return Returns.Message(HttpStatusCode.NotFound, "");
        if (!string.IsNullOrEmpty(map.StoryboardSHA256Hash) && map.StoryboardSHA256Hash != payload.StoryboardHash)
            return Returns.Message(HttpStatusCode.NotFound, "");

        var mods = payload.Mods.Select(ModUtils.GetFromAcronym).ToList();
        if (mods.Any(x => x is null || !x.Rankable))
            return Returns.Message(HttpStatusCode.BadRequest, "");

        float duration = map.Length;

        if (mods.FirstOrDefault(x => x is RateMod) is RateMod r)
            duration /= r.Rate;

        var completion = DateTimeOffset.Now.AddMilliseconds(duration);
        var ticket = RandomizeUtils.GenerateRandomString(20);
        module.Tickets[ticket] = new ScoreSubmissionModule.Ticket(map.ID, completion);
        return ticket;
    }

    [Authenticated, HttpRoute("/", APIMethod.Post)]
    public APIReturn<ScoreSubmissionStats> Submit(User auth, [Source(ParameterSource.Body)] ScoreSubmissionPayload payload)
    {
        if (auth.HasFlag(UserBanFlag.LeaderboardBan))
            return Returns.Message(HttpStatusCode.Forbidden, "You are banned from submitting scores.");

        var submission = payload.Scores?.FirstOrDefault();

        if (submission is null || payload.Mods is null || payload.Replay is null || string.IsNullOrEmpty(payload.MapHash))
            return invalid();

        var map = maps.GetMapByHash(payload.MapHash);

        if (map == null)
            return Returns.Message(HttpStatusCode.NotFound, "This map is not uploaded to the server!");
        if (!string.IsNullOrEmpty(map.EffectSHA256Hash) && map.EffectSHA256Hash != payload.EffectHash)
            return invalid();

        var mapset = maps.GetSet(map.SetID);
        if (mapset == null) throw new InvalidOperationException($"The parent MapSet {map.SetID} of Map {map.ID} does not exist!");

        if (!module.Tickets.TryGetValue(payload.Ticket, out var ticket) || ticket.MapID != map.ID)
            return invalid();

        var currentFirst = scores.GetFirst(map.ID);

        var mods = payload.Mods.Select(ModUtils.GetFromAcronym).ToList();
        if (mods.Any(x => x is null || !x.Rankable)) return invalid();

        Logger.Log($"{ticket.PredictedCompletion} {DateTimeOffset.Now}");
        var delta = DateTimeOffset.Now - ticket.PredictedCompletion;
        if (delta > TimeSpan.FromSeconds(30)) return invalid();

        var accuracy = map.AccuracyDifficulty;
        var rate = 1f;

        if (mods.Any(x => x is HardMod))
            accuracy *= 1.5f;
        if (mods.FirstOrDefault(x => x is RateMod) is RateMod r)
            rate = r.Rate;

        var hitWindow = new HitWindows(accuracy, rate);
        var releaseWindow = new ReleaseWindows(accuracy, rate);
        var landmineWindow = new LandmineWindows(accuracy, rate);

        var results = new Dictionary<Judgement, int>();
        var combo = 0;
        var max = 0;

        foreach (var result in submission.Results)
        {
            var window = result.Type switch
            {
                ResultType.HoldEnd => releaseWindow,
                ResultType.Landmine => landmineWindow,
                _ => hitWindow
            };

            var judge = window.JudgementFor(result.Difference);

            if (!results.TryAdd(judge, 1))
                results[judge]++;

            if (judge > hitWindow.ComboBreakJudgement)
                combo++;
            else
                combo = 0;

            max = Math.Max(max, combo);
        }

        var total = results.Sum(x => x.Value);

        if (total != map.MaxCombo)
            return invalid();

        var score = new Score
        {
            UserID = auth.ID,
            MapID = map.ID,
            MapHash = payload.MapHash,
            Time = DateTimeOffset.Now,
            Mods = string.Join(',', payload.Mods),

            MaxCombo = max,
            FlawlessCount = results.GetValueOrDefault(Judgement.Flawless, 0),
            PerfectCount = results.GetValueOrDefault(Judgement.Perfect, 0),
            GreatCount = results.GetValueOrDefault(Judgement.Great, 0),
            AlrightCount = results.GetValueOrDefault(Judgement.Alright, 0),
            OkayCount = results.GetValueOrDefault(Judgement.Okay, 0),
            MissCount = results.GetValueOrDefault(Judgement.Miss, 0),

            ScrollSpeed = submission.ScrollSpeed
        };

        score.Recalculate(maps);
        scores.Add(score);

        var replayBuffer = Encoding.UTF8.GetBytes(payload.Replay.Serialize());
        Assets.WriteAsset(AssetType.Replay, score.ID, replayBuffer);

        if (!mapset.AllowScores())
            return Returns.Message(HttpStatusCode.BadRequest, "This map is not pure!");

        var ovr = auth.OverallRating;
        var ptr = auth.PotentialRating;
        var rank = auth.GetGlobalRank(cache);

        users.UpdateLocked(auth.ID, u => u.Recalculate(scores, maps, cache));
        tasks.Schedule(new MethodTask(() => events.FirstPlace(map, currentFirst)));

        if (score.ScrollSpeed <= 2.05f && Math.Abs(rate - 0.5) < 0.001f && map.Length >= 4 * 60 * 1000)
            tasks.Schedule(new RewardAchievementTask(auth.ID, AchievementList.SnailPace.ID));

        var club = auth.GetClub(clubs);

        if (club != null)
        {
            tasks.Schedule(new RecalculateClubTask(club.ID));
            tasks.Schedule(new RecalculateClubScoreTask(map.ID, club.ID));
            tasks.Schedule(new RefreshClubClaimTask(map.ID));
        }

        return new ScoreSubmissionStats(
            translator.ToAPI(score),
            ovr, ptr, rank,
            auth.OverallRating, auth.PotentialRating, auth.GetGlobalRank(cache)
        );

        StatusReturn invalid() => Returns.Message(HttpStatusCode.BadRequest, "Failed to verify score.");
    }
}
*/
