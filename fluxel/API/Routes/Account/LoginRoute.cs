using System.Net;
using fluxel.API.Components;
using fluxel.API.Utils;
using fluxel.Database.Helpers;

namespace fluxel.API.Routes.Account;

public class LoginRoute : IApiRoute {
    public string Path => "/account/login";
    public string Method => "GET";

    public ApiResponse Handle(HttpListenerRequest req, HttpListenerResponse res, Dictionary<string, string> parameters) {
        var username = req.Headers["username"];
        var password = req.Headers["password"];

        if (username == null || password == null) {
            return new ApiResponse {
                Status = HttpStatusCode.BadRequest,
                Message = "Missing username or password"
            };
        }

        var user = UserHelper.Get(username);

        if (user == null) {
            return new ApiResponse {
                Status = HttpStatusCode.BadRequest,
                Message = "No user with that username"
            };
        }

        if (!PasswordUtils.VerifyPassword(password, user.Password)) {
            return new ApiResponse {
                Status = HttpStatusCode.BadRequest,
                Message = "The provided password is incorrect"
            };
        }

        return new ApiResponse {
            Data = user.ToShort()
        };
    }
}
