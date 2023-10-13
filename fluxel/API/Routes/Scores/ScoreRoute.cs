using System.Net;
using fluxel.API.Components;
using fluxel.Constants;
using fluxel.Database.Helpers;

namespace fluxel.API.Routes.Scores;

public class ScoreRoute : IApiRoute {
    public string Path => "/scores/id/:id";
    public string Method => "GET";

    public ApiResponse Handle(HttpListenerRequest req, HttpListenerResponse res, Dictionary<string, string> parameters) {
        if (!int.TryParse(parameters["id"], out var id)) {
            return new ApiResponse {
                Status = HttpStatusCode.BadRequest,
                Message = ResponseStrings.InvalidParameter("id", "integer")
            };
        }

        var score = ScoreHelper.Get(id);

        if (score == null) {
            return new ApiResponse {
                Status = HttpStatusCode.NotFound,
                Message = ResponseStrings.ScoreNotFound
            };
        }

        return new ApiResponse {
            Data = score
        };
    }
}
