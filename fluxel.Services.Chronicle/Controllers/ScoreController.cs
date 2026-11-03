using System.Linq;
using fluxel.Components;
using fluxel.Models;
using fluxel.Models.Users;
using fluXis.Online.API.Responses.Maps;
using Microsoft.EntityFrameworkCore;
using Midori.API.Attributes;
using Midori.API.Components;
using Midori.Networking;

namespace fluxel.Services.Chronicle.Controllers;

[Controller("/scores")]
public class ScoreController
{
    private readonly DatabaseContext database;
    private readonly ModelTranslator translator;

    public ScoreController(DatabaseContext database, ModelTranslator translator)
    {
        this.database = database;
        this.translator = translator;
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
                     .Select(g => g.OrderByDescending(x => x.PerformanceRating).First())
                     .Take(50);

        return new MapLeaderboard(
            translator.ToAPI(map.MapSet!),
            translator.ToAPI(map),
            query.ToArray().Select(x => translator.ToAPI(x))
        );
    }
}
