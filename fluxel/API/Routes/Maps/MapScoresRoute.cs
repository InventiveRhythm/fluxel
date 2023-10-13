using System.Net;
using fluxel.API.Components;
using fluxel.Components.Scores;
using fluxel.Constants;
using fluxel.Database.Helpers;

namespace fluxel.API.Routes.Maps;

public class MapScoresRoute : IApiRoute {
    public string Path => "/map/:id/scores";
    public string Method => "GET";

    public ApiResponse Handle(HttpListenerRequest req, HttpListenerResponse res, Dictionary<string, string> parameters) {
        if (!int.TryParse(parameters["id"], out var id)) {
            return new ApiResponse {
                Status = HttpStatusCode.BadRequest,
                Message = ResponseStrings.InvalidParameter("id", "integer")
            };
        }

        var map = MapHelper.Get(id);

        if (map == null) {
            return new ApiResponse {
                Status = HttpStatusCode.NotFound,
                Message = ResponseStrings.MapNotFound
            };
        }

        var all = ScoreHelper.FromMap(map).OrderByDescending(s => s.TotalScore).ToList();

        var scores = new List<Score>();

        foreach (var score in all) {
            if (scores.Count >= 50)
                break;

            if (scores.Any(s => s.UserId == score.UserId))
                continue;

            scores.Add(score);
        }

        return new ApiResponse {
            Data = new {
                scores,
                map = map.ToShort()
            }
        };
    }
}
