using System.Net;
using fluxel.API.Components;
using fluxel.Constants;
using fluxel.Database.Helpers;

namespace fluxel.API.Routes.Users;

public class UserScoresRoute : IApiRoute
{
    public string Path => "/user/:id/scores";
    public string Method => "GET";

    public ApiResponse Handle(HttpListenerRequest req, HttpListenerResponse res, Dictionary<string, string> parameters)
    {
        if (int.TryParse(parameters["id"], out var id) == false) {
            return new ApiResponse {
                Status = HttpStatusCode.BadRequest,
                Message = ResponseStrings.InvalidParameter("id", "integer")
            };
        }

        var user = UserHelper.Get(id);

        if (user == null) {
            return new ApiResponse {
                Status = HttpStatusCode.NotFound,
                Message = ResponseStrings.UserNotFound
            };
        }

        return new ApiResponse {
            Data = new
            {
                recent_scores = user.RecentScores,
                best_scores = user.BestScores
            }
        };
    }
}
