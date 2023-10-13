using System.Net;
using fluxel.API.Components;
using fluxel.Database.Helpers;

namespace fluxel.API.Routes.Stats.User;

public class UserCreationStatsRoute : IApiRoute
{
    public string Path => "/stats/users/creation";
    public string Method => "GET";

    public ApiResponse? Handle(HttpListenerRequest req, HttpListenerResponse res, Dictionary<string, string> parameters)
    {
        return new ApiResponse { Data = UserHelper.All.Where(u => u.CreatedAt > 0).Select(u => u.CreatedAt) };
    }
}
