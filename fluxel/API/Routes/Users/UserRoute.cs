using System.Net;
using fluxel.API.Components;
using fluxel.Components.Users;
using fluxel.Constants;
using fluxel.Database;

namespace fluxel.API.Routes.Users; 

public class UserRoute : IApiRoute {
    public string Path => "/user/:id";
    public string Method => "GET";
    public ApiResponse? Handle(HttpListenerRequest req, HttpListenerResponse res, Dictionary<string, string> parameters) {
        if (int.TryParse(parameters["id"], out var id) == false) {
            return new ApiResponse {
                Status = HttpStatusCode.BadRequest,
                Message = ResponseStrings.InvalidParameter("id", "integer")
            };
        }

        return RealmAccess.Run(realm => {
            var user = realm.Find<User>(id);
            
            if (user == null) {
                return new ApiResponse {
                    Status = HttpStatusCode.NotFound,
                    Message = ResponseStrings.UserNotFound
                };
            }
            
            return new ApiResponse {
                Data = user
            };
        });
    }
}