using System.Net;
using fluxel.API.Components;
using fluxel.Components.Users;
using fluxel.Database;

namespace fluxel.API.Routes.Users;

public class UsersRoute : IApiRoute {
    public string Path => "/users";
    public string Method => "GET";

    public ApiResponse Handle(HttpListenerRequest req, HttpListenerResponse res, Dictionary<string, string> parameters) {
        return RealmAccess.Run(realm => {
            return new ApiResponse {
                Data = new {
                    users = realm.All<User>().ToList().Select(user => user.ToShort())
                }
            };
        });
    }
}
