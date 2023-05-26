using System.Net;
using fluxel.API.Components;
using fluxel.Components.Users;
using fluxel.Database;

namespace fluxel.API.Routes.Users; 

public class UserRoute : IApiRoute {
    public string Path => "/user/:id";
    public string Method => "GET";
    public ApiResponse? Handle(HttpListenerRequest req, HttpListenerResponse res, Dictionary<string, string> parameters) {
        if (int.TryParse(parameters["id"], out var id) == false) {
            return new ApiResponse {
                Status = 400,
                Message = "Invalid user ID"
            };
        }

        return RealmAccess.Run(realm => {
            var user = realm.Find<User>(id);
            
            if (user == null) {
                return new ApiResponse {
                    Status = 404,
                    Message = "User not found"
                };
            }
            
            return new ApiResponse {
                Data = user
            };
        });
    }
}