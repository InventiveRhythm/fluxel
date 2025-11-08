using System.Globalization;
using fluxel.API.Components;
using fluxel.Constants;
using fluxel.Database.Helpers;
using fluxel.Models.Maps;
using fluxel.Models.Scores;
using fluxel.Models.Users;
using fluXis.Online.API.Models.Scores;
using fluXis.Online.API.Models.Users;
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
        Console.WriteLine("parsed payload successfully");
        Console.WriteLine("mods : ");

        foreach (var payloadMod in payload.Mods)
        {
            Console.WriteLine("mod: " + payloadMod);

            if (payloadMod.EndsWith("x"))
            {
                var numberPart = payloadMod[..^1];
                Console.WriteLine("Number: " + numberPart);

                if (float.TryParse(numberPart, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
                {
                    rate = parsed;
                    Console.WriteLine("rate set to: " + rate);
                }
            }
        }

        Console.WriteLine("rate set to: " + rate);

        Map? map = MapHelper.GetByHash(payload.MapHash);

        if (map == null)
        {
            Console.WriteLine("Couldn't find map from hash: " + payload.MapHash);
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, ResponseStrings.MapHashNotFound);
            return;
        }

        Console.WriteLine("Map " + map.SetID + " (" + map.FileName + ")");

        if (payload.Scores.Count == 0)
        {
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, "score contains no players");
            return;
        }

        //only submit handle player 1 for now
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
            Console.WriteLine("user with id: " + userScore.UserID);
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

        //recalculate ptr/ovr/rank
        try
        {
            UserHelper.UpdateLocked(userScore.UserID, u => u.Recalculate());
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed to update user: " + e.Message);
            await interaction.ReplyMessage(HttpStatusCode.InternalServerError, "failed to recalculate user stats");
            return;
        }

        //get new stats (might not be needed if the previous one somehow gets updated?)
        user = UserHelper.Get(userScore.UserID);

        if (user == null)
        {
            Console.WriteLine("user with id: " + userScore.UserID);
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, "failed to get user");
            return;
        }

        ScoreSubmissionStats response = makeScoreSubmissionStats(userScore, prevOvr, prevPrt, prevRank, user.OverallRating, user.PotentialRating, user.GetGlobalRank());

        //TODO: handle replay

        await interaction.Reply(HttpStatusCode.OK, response);
    }

    private ScoreSubmissionStats makeScoreSubmissionStats(Score score, double prevOvr, double prevPtr, int prevRank, double curOvr, double curPtr, int curRank)
    {
        return new ScoreSubmissionStats(
            new APIScore
            {
                ID = score.ID,
                Accuracy = score.Accuracy,
                AlrightCount = score.AlrightCount,
                FlawlessCount = score.FlawlessCount,
                PerfectCount = score.PerfectCount,
                OkayCount = score.OkayCount,
                MissCount = score.MissCount,
                MaxCombo = score.MaxCombo,
                ScrollSpeed = score.ScrollSpeed,
                Rank = score.Grade,
                PerformanceRating = score.PerformanceRating,
                Time = score.TimeLong,
                User = new APIUser
                {
                    ID = score.UserID,
                    Username = score.User.Username, //should be fine since we checked if the user existed before reaching this
                }
            },
            prevOvr,
            prevPtr,
            prevRank,
            curOvr,
            curPtr,
            curRank);
    }
}
