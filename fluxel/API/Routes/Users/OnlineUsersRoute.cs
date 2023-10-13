using System.Net;
using fluxel.API.Components;
using fluxel.Database.Helpers;

namespace fluxel.API.Routes.Users;

public class OnlineUsersRoute : IApiRoute {
    public string Path => "/users/online";
    public string Method => "GET";

    public ApiResponse Handle(HttpListenerRequest req, HttpListenerResponse res, Dictionary<string, string> parameters) {
        return new ApiResponse {
            Data = new {
                count = GlobalStatistics.Online,
                users = GlobalStatistics.GetOnlineUsers.Select(u => UserHelper.Get(u)?.ToShort())
            }
        };
    }
}
