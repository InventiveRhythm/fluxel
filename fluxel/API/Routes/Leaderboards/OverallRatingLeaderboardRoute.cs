using System.Net;
using fluxel.API.Components;
using fluxel.Components.Users;
using fluxel.Database;

namespace fluxel.API.Routes.Leaderboards;

public class OverallRatingLeaderboardRoute : IApiRoute
{
    public string Path => "/leaderboards/overall";
    public string Method => "GET";

    public ApiResponse? Handle(HttpListenerRequest req, HttpListenerResponse res, Dictionary<string, string> parameters)
    {
        return RealmAccess.Run(realm => new ApiResponse
        {
            Data = realm.All<User>().OrderByDescending(u => u.OverallRating).Take(100).ToList()
        });
    }
}
