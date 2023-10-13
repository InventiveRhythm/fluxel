using System.Net;
using fluxel.API.Components;
using fluxel.Database.Helpers;

namespace fluxel.API.Routes.Leaderboards;

public class OverallRatingLeaderboardRoute : IApiRoute
{
    public string Path => "/leaderboards/overall";
    public string Method => "GET";

    public ApiResponse? Handle(HttpListenerRequest req, HttpListenerResponse res, Dictionary<string, string> parameters)
    {
        return new ApiResponse
        {
            Data = UserHelper.All.OrderByDescending(u => u.OverallRating).Take(100).ToList()
        };
    }
}
