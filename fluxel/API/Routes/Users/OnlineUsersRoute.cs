using System.Net;
using fluxel.API.Components;
using fluxel.Components.Users;

namespace fluxel.API.Routes.Users;

public class OnlineUsersRoute : IApiRoute {
    public string Path => "/users/online";
    public string Method => "GET";

    public ApiResponse Handle(HttpListenerRequest req, HttpListenerResponse res, Dictionary<string, string> parameters) {
        return new ApiResponse {
            Data = new {
                count = Stats.Online,
                users = Stats.GetOnlineUsers.Select(u => User.FindById(u)?.ToShort())
            }
        };
    }
}
