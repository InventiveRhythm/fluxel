using System.Net;
using fluxel.API.Components;
using fluxel.Components.Users;
using fluxel.Database;

namespace fluxel.API.Routes.Users; 

public class UsersRoute : IApiRoute {
    public string Path => "/users";
    public string Method => "GET";
    
    public ApiResponse? Handle(HttpListenerRequest req, HttpListenerResponse res, Dictionary<string, string> parameters) {
        return RealmAccess.Run(realm => {
            var users = realm.All<User>();
            var userShorts = new List<UserShort>();
            
            foreach (var user in users) {
                userShorts.Add(user.ToShort());
            }
            
            return new ApiResponse {
                Data = new Dictionary<string, object> {
                    { "users", userShorts }
                }
            };
        });
    }
}