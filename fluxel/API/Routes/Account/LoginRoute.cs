using System.Net;
using fluxel.API.Components;
using fluxel.API.Utils;
using fluxel.Components.Users;
using fluxel.Database;

namespace fluxel.API.Routes.Account; 

public class LoginRoute : IApiRoute {
    public string Path => "/account/login";
    public string Method => "GET";
    
    public ApiResponse? Handle(HttpListenerRequest req, HttpListenerResponse res, Dictionary<string, string> parameters) {
        var username = req.Headers["username"];
        var password = req.Headers["password"];
        
        if (username == null || password == null) {
            return new ApiResponse {
                Status = 400,
                Message = "Missing username or password"
            };
        }

        return RealmAccess.Run(realm => {
            var users = realm.All<User>();

            foreach (var user in users) {
                if (user.Username == username && PasswordUtils.VerifyPassword(password, user.Password)) {
                    return new ApiResponse {
                        Data = user.ToShort()
                    };
                }
            }
            
            return new ApiResponse {
                Status = 400,
                Message = "Invalid username or password"
            };
        });
    }
}