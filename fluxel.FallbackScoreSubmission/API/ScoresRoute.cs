using System.Globalization;
using System.Text;
using System.Text.Json;
using fluxel.API.Components;
using fluxel.Constants;
using fluxel.Database.Extensions;
using fluxel.Database.Helpers;
using fluxel.Models.Maps;
using fluxel.Models.Scores;
using fluxel.Models.Users;
using fluXis.Online.API.Payloads.Scores;
using fluXis.Online.API.Responses.Scores;
using fluXis.Scoring;
using fluXis.Scoring.Enums;
using Midori.Networking;

namespace fluxel.FallbackScoreSubmission.API;

public class ScoresRoute : IFluxelAPIRoute
{
    public string RoutePath => "/scores";
    public HttpMethod Method => HttpMethod.Post;

    public async Task Handle(FluxelAPIInteraction interaction)
    {
        if (!interaction.TryParseBody<ScoreSubmissionPayload>(out var payload))
        {
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, ResponseStrings.InvalidBodyJson);
            return;
        }

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

        Map? map = MapHelper.GetByHash(payload.MapHash);

        if (map == null)
        {
            Console.WriteLine("Couldn't find map from hash: " + payload.MapHash);
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, ResponseStrings.MapHashNotFound);
            return;
        }

        if (payload.Scores.Count == 0)
        {
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, "score contains no players");
            return;
        }

        //only handle the first player for now
        Score userScore = new Score
        {
            UserID = payload.Scores[0].UserID,
            MapHash = payload.MapHash,
            MapID = map.ID,
            ScrollSpeed = payload.Scores[0].ScrollSpeed,
            Mods = string.Join(",", payload.Mods),
        };

        User? user = UserHelper.Get(userScore.UserID);

        if (user == null)
        {
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, "failed to get user");
            return;
        }

        //get user old stats
        double prevOvr = user.OverallRating;
        double prevPrt = user.PotentialRating;
        int prevRank = user.GetGlobalRank();

        //handle results
        HitWindows hitWindows = new HitWindows(map.AccuracyDifficulty, rate);
        ReleaseWindows releaseWindows = new ReleaseWindows(map.AccuracyDifficulty, rate);
        int combo = 0;
        int maxCombo = 0;
        int judgementCount = 0;

        foreach (var result in payload.Scores[0].Results)
        {
            Judgement judgement = result.HoldEnd
                ? releaseWindows.JudgementFor(result.Difference)
                : hitWindows.JudgementFor(result.Difference);

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
        if (judgementCount != userScore.Map.MaxCombo)
        {
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, "judgement count doesn't match the map's hit object count");
            return;
        }

        //submit score
        userScore.MaxCombo = maxCombo;
        userScore.Recalculate();
        ScoreHelper.Add(userScore);

        //save replay
        string replayJson = JsonSerializer.Serialize(payload.Replay);
        var replayBytes = Encoding.UTF8.GetBytes(replayJson);
        Assets.WriteAsset(AssetType.Replay, $"{userScore.ID}", replayBytes, "", "frp");

        //recalculate ptr/ovr/rank
        try
        {
            UserHelper.UpdateLocked(userScore.UserID, u => u.Recalculate());
        }
        catch (Exception e)
        {
            await interaction.ReplyMessage(HttpStatusCode.InternalServerError, "failed to recalculate user stats");
            return;
        }

        //get new stats (might not be needed if the previous one somehow gets updated?)
        user = UserHelper.Get(userScore.UserID);

        if (user == null)
        {
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, "failed to get user");
            return;
        }

        ScoreSubmissionStats response = new ScoreSubmissionStats(userScore.ToAPI(), prevOvr, prevPrt, prevRank, user.OverallRating, user.PotentialRating, user.GetGlobalRank());

        await interaction.Reply(HttpStatusCode.OK, response);
    }
}
