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

        if (payload.Scores.Count != map.PlayerCount)
        {
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, "invalid player count");
            return;
        }

        Score userScore = createScoreFromPayload(payload, map, rate);

        if (!allPlayersValid(userScore))
        {
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, "invalid users in score"); //log more info?
            return;
        }

        //get users old stats
        List<UserStats> previousUserStats = new()
        {
            new()
            {
                Ovr = userScore.User.OverallRating,
                Prt = userScore.User.PotentialRating,
                Rank = userScore.User.GetGlobalRank()
            }
        };

        foreach (var extraPlayer in userScore.ExtraPlayers)
        {
            previousUserStats.Add(new()
            {
                Ovr = extraPlayer.User.OverallRating,
                Prt = extraPlayer.User.PotentialRating,
                Rank = extraPlayer.User.GetGlobalRank(),
            });
        }

        //check judgement counts
        if (userScore.JudgementCount != userScore.Map.MaxComboForPlayer(0))
        {
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, "player 0 judgement count doesn't match the map's hit object count");
            return;
        }

        int playerIndex = 1;

        foreach (var extraPlayer in userScore.ExtraPlayers)
        {
            if (extraPlayer.JudgementCount != userScore.Map.MaxComboForPlayer(playerIndex))
            {
                //Console.WriteLine("player " + playerIndex + " judgement count doesn't match the map's hit object count");
                //Console.WriteLine("expected " + userScore.Map.MaxComboForPlayer(playerIndex) + ", got " + extraPlayer.JudgementCount);
                await interaction.ReplyMessage(HttpStatusCode.BadRequest, "player " + playerIndex + " judgement count doesn't match the map's hit object count");
                return;
            }

            playerIndex++;
        }

        //submit score
        ScoreHelper.Add(userScore);

        //save replay
        string replayJson = JsonSerializer.Serialize(payload.Replay);
        var replayBytes = Encoding.UTF8.GetBytes(replayJson);
        Assets.WriteAsset(AssetType.Replay, $"{userScore.ID}", replayBytes, "", "frp");

        //recalculate ptr/ovr/rank
        try
        {
            UserHelper.UpdateLocked(userScore.UserID, u => u.Recalculate());
            foreach (var extraPlayer in userScore.ExtraPlayers) UserHelper.UpdateLocked(extraPlayer.UserID, u => u.Recalculate());
        }
        catch (Exception e)
        {
            await interaction.ReplyMessage(HttpStatusCode.InternalServerError, "failed to recalculate user stats");
            return;
        }

        //get new stats  TODO: adpapt this for multiple players?
        User? user = UserHelper.Get(userScore.UserID);

        if (user == null)
        {
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, "failed to get user");
            return;
        }

        ScoreSubmissionStats response = new ScoreSubmissionStats(userScore.ToAPI(), previousUserStats[0].Ovr, previousUserStats[0].Prt, previousUserStats[0].Rank, user.OverallRating, user.PotentialRating, user.GetGlobalRank());

        await interaction.Reply(HttpStatusCode.OK, response);
    }

    private Score createScoreFromPayload(ScoreSubmissionPayload payload, Map map, float rate)
    {
        Score userScore = new Score
        {
            UserID = payload.Scores[0].UserID,
            MapHash = payload.MapHash,
            MapID = map.ID,
            ScrollSpeed = payload.Scores[0].ScrollSpeed,
            Mods = string.Join(",", payload.Mods),
        };
        for (int i = 1; i < payload.Scores.Count; i++) userScore.ExtraPlayers.Add(new() { UserID = payload.Scores[i].UserID, Score = userScore });

        HitWindows hitWindows = new HitWindows(map.AccuracyDifficulty, rate);
        ReleaseWindows releaseWindows = new ReleaseWindows(map.AccuracyDifficulty, rate);
        int combo = 0;
        int maxCombo = 0;

        //first user
        foreach (var result in payload.Scores[0].Results)
        {
            Judgement judgement = result.HoldEnd
                ? releaseWindows.JudgementFor(result.Difference)
                : hitWindows.JudgementFor(result.Difference);

            combo++;

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

        userScore.MaxCombo = maxCombo;
        userScore.Recalculate();

        //all other users
        for (int i = 1; i < payload.Scores.Count; i++)
        {
            ScoreExtraPlayer scoreExtraPlayer = userScore.ExtraPlayers[i - 1];

            combo = 0;
            maxCombo = 0;

            foreach (var result in payload.Scores[i].Results)
            {
                Judgement judgement = result.HoldEnd
                    ? releaseWindows.JudgementFor(result.Difference)
                    : hitWindows.JudgementFor(result.Difference);

                combo++;

                switch (judgement)
                {
                    case Judgement.Flawless: scoreExtraPlayer.FlawlessCount++; break;

                    case Judgement.Perfect: scoreExtraPlayer.PerfectCount++; break;

                    case Judgement.Alright: scoreExtraPlayer.AlrightCount++; break;

                    case Judgement.Great: scoreExtraPlayer.GreatCount++; break;

                    case Judgement.Okay: scoreExtraPlayer.OkayCount++; break;

                    case Judgement.Miss:
                        combo = 0;
                        scoreExtraPlayer.MissCount++;
                        break;
                }

                if (combo > maxCombo) maxCombo = combo;
            }

            scoreExtraPlayer.MaxCombo = maxCombo;
            scoreExtraPlayer.Recalculate(i);
        }

        return userScore;
    }

    private bool allPlayersValid(Score score)
    {
        if (score.User == null) return false;

        foreach (var extraPlayer in score.ExtraPlayers)
        {
            if (extraPlayer.User == null) return false;

            //if (score.UserID == extraPlayer.UserID) return false; //uncomment this to prevent local scores from being submitted (we might want to do also do that client side)
        }

        return true;
    }

    public class UserStats
    {
        public double Ovr { get; set; }
        public double Prt { get; set; }
        public int Rank { get; set; }

        public UserStats()
        {
        }
    }
}
