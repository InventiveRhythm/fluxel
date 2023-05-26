using System.Net;
using fluxel.API.Components;
using fluxel.API.Utils;
using fluxel.Components.Users;
using fluxel.Database;
using Newtonsoft.Json;

namespace fluxel.API.Routes.Account;

public class RegisterRoute : IApiRoute {
    public string Path => "/account/register";
    public string Method => "POST";
    
    public ApiResponse? Handle(HttpListenerRequest req, HttpListenerResponse res, Dictionary<string, string> parameters) {
        string body = new StreamReader(req.InputStream).ReadToEnd();
        var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(body);
        
        if (data == null) {
            return new ApiResponse {
                Status = 400,
                Data = "Invalid JSON"
            };
        }
        
        if (!data.ContainsKey("username") || !data.ContainsKey("password")) {
            return new ApiResponse {
                Status = 400,
                Data = "Missing username or password"
            };
        }
        
        var username = data["username"];
        var password = data["password"];
        
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)) {
            return new ApiResponse {
                Status = 400,
                Data = "Username or password is empty"
            };
        }
        
        if (username.Length is < 3 or > 16) {
            return new ApiResponse {
                Status = 400,
                Data = "Username must be between 3 and 16 characters"
            };
        }
        
        if (password.Length is < 8 or > 32) {
            return new ApiResponse {
                Status = 400,
                Data = "Password must be between 8 and 32 characters"
            };
        }
        
        if (User.UsernameExists(username)) {
            return new ApiResponse {
                Status = 400,
                Data = "Username is already taken"
            };
        }
        
        var user = new User {
            Id = User.GetNextId(),
            Username = username,
            Password = PasswordUtils.HashPassword(password)
        };
        
        return new ApiResponse {
            Data = RealmAccess.Run(realm => realm.Add(user)).ToShort()
        };
    }
}