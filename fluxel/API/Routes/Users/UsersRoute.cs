using System.Net;
using fluxel.API.Components;
using fluxel.Database.Helpers;

namespace fluxel.API.Routes.Users;

public class UsersRoute : IApiRoute {
    public string Path => "/users";
    public string Method => "GET";

    public ApiResponse Handle(HttpListenerRequest req, HttpListenerResponse res, Dictionary<string, string> parameters)
    {
        return new ApiResponse { Data = UserHelper.All };
    }
}
