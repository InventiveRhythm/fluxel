using System.Globalization;
using System.Text;
using System.Text.Json;
using fluxel.Components;
using fluxel.Constants;
using fluxel.Database;
using fluxel.Models.Maps;
using fluxel.Models.Scores;
using fluxel.Models.Users;
using fluXis.Online.API.Payloads.Scores;
using fluXis.Online.API.Responses.Scores;
using fluXis.Scoring;
using fluXis.Scoring.Enums;
using Midori.API.Attributes;
using Midori.API.Components;
using Midori.Networking;
using ScoreManager = fluxel.Database.ScoreManager;

namespace fluxel.FallbackScoreSubmission;

[Controller("/scores")]
public class FallbackScoreController
{
    private readonly MapManager maps;
    private readonly UserManager users;
    private readonly ScoreManager scores;
    private readonly ModelTranslator translator;

    public FallbackScoreController(MapManager maps, UserManager users, ModelTranslator translator, ScoreManager scores)
    {
        this.maps = maps;
        this.users = users;
        this.translator = translator;
        this.scores = scores;
    }

    [Authenticated, HttpRoute("/", APIMethod.Post)]
    public APIReturn<ScoreSubmissionStats> Submit(User auth, [Source(ParameterSource.Body)] ScoreSubmissionPayload payload)
    {
        float rate = 1f;

        foreach (var payloadMod in payload.Mods)
        {
            if (payloadMod.EndsWith("x"))
            {
                var numberPart = payloadMod[..^1];

                if (float.TryParse(numberPart, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
                {
                    rate = parsed;
                }
            }
        }

        Map? map = maps.GetMapByHash(payload.MapHash);

        if (map == null)
        {
            Console.WriteLine("Couldn't find map from hash: " + payload.MapHash);
            return Returns.Message(HttpStatusCode.BadRequest, ResponseStrings.MapHashNotFound);
        }

        if (payload.Scores.Count == 0)
            return Returns.Message(HttpStatusCode.BadRequest, "score contains no players");

        //only handle the first player for now
        Score userScore = new Score
        {
            UserID = payload.Scores[0].UserID,
            MapHash = payload.MapHash,
            MapID = map.ID,
            ScrollSpeed = payload.Scores[0].ScrollSpeed,
            Mods = string.Join(",", payload.Mods),
        };

        User? user = users.Get(userScore.UserID);

        if (user == null)
            return Returns.Message(HttpStatusCode.BadRequest, "failed to get user");

        //get user old stats
        double prevOvr = user.OverallRating;
        double prevPrt = user.PotentialRating;
        int prevRank = user.GetGlobalRank(translator.Cache);

        //handle results
        HitWindows hitWindows = new HitWindows(map.AccuracyDifficulty, rate);
        ReleaseWindows releaseWindows = new ReleaseWindows(map.AccuracyDifficulty, rate);
        LandmineWindows landmineWindows = new LandmineWindows(map.AccuracyDifficulty, rate);
        int combo = 0;
        int maxCombo = 0;
        int judgementCount = 0;

        foreach (var result in payload.Scores[0].Results)
        {
            var window = result.Type switch
            {
                ResultType.HoldEnd => releaseWindows,
                ResultType.Landmine => landmineWindows,
                _ => hitWindows
            };

            Judgement judgement = window.JudgementFor(result.Difference);

            combo++;
            judgementCount++;

            switch (judgement)
            {
                case Judgement.Flawless: userScore.FlawlessCount++; break;

                case Judgement.Perfect: userScore.PerfectCount++; break;

                case Judgement.Alright: userScore.AlrightCount++; break;

                case Judgement.Great: userScore.GreatCount++; break;

                case Judgement.Okay: userScore.OkayCount++; break;

                case Judgement.Miss:
                    combo = 0;
                    userScore.MissCount++;
                    break;
            }

            if (combo > maxCombo) maxCombo = combo;
        }

        //make sure the judgement count is correct
        if (judgementCount != maps.GetMap(userScore.MapID)?.MaxCombo)
            return Returns.Message(HttpStatusCode.BadRequest, "judgement count doesn't match the map's hit object count");

        //submit score
        userScore.MaxCombo = maxCombo;
        userScore.Recalculate(maps);
        scores.Add(userScore);

        //save replay
        string replayJson = JsonSerializer.Serialize(payload.Replay);
        var replayBytes = Encoding.UTF8.GetBytes(replayJson);
        Assets.WriteAsset(AssetType.Replay, $"{userScore.ID}", replayBytes, "", "frp");

        //recalculate ptr/ovr/rank
        try
        {
            users.UpdateLocked(userScore.UserID, u => u.Recalculate(scores, maps, translator.Cache));
        }
        catch (Exception e)
        {
            return Returns.Message(HttpStatusCode.InternalServerError, "failed to recalculate user stats");
        }

        //get new stats (might not be needed if the previous one somehow gets updated?)
        user = users.Get(userScore.UserID);

        if (user == null)
            return Returns.Message(HttpStatusCode.BadRequest, "failed to get user");

        ScoreSubmissionStats response =
            new ScoreSubmissionStats(translator.ToAPI(userScore), prevOvr, prevPrt, prevRank, user.OverallRating, user.PotentialRating, user.GetGlobalRank(translator.Cache));

        return response;
    }
}
