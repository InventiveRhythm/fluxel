using System.Net;
using System.Text.RegularExpressions;
using fluxel.API.Components;
using fluxel.Components.Users;
using fluxel.Constants;
using fluxel.Database;
using fluxel.Utils;

namespace fluxel.API.Routes.Account; 

public class AccountUpdateRoute : IApiRoute {
    public string Path => "/account/update/:action";
    public string Method => "POST";
    
    public ApiResponse Handle(HttpListenerRequest req, HttpListenerResponse res, Dictionary<string, string> parameters) {
        var action = parameters["action"];
        var token = req.Headers["Authorization"];
        
        if (token == null) {
            return new ApiResponse {
                Status = HttpStatusCode.Unauthorized,
                Message = ResponseStrings.NoToken
            };
        }
        
        var userToken = UserToken.GetByToken(token);
        
        if (userToken == null) {
            return new ApiResponse {
                Status = HttpStatusCode.Unauthorized,
                Message = ResponseStrings.InvalidToken
            };
        }
        
        var user = User.FindById(userToken.UserId);
        
        if (user == null) {
            return new ApiResponse {
                Status = HttpStatusCode.Unauthorized,
                Message = ResponseStrings.TokenUserNotFound
            };
        }

        switch (action) {
            case "displayname":
                var name = new StreamReader(req.InputStream).ReadToEnd();
                
                // check if name is valid (3-20 characters, no special characters) 
                if (!Regex.IsMatch(name, @"^[a-zA-Z0-9_ ]{1,20}$")) {
                    return new ApiResponse {
                        Message = "Invalid display name",
                        Status = HttpStatusCode.BadRequest
                    };
                }

                RealmAccess.Run(realm => {
                    var users = realm.Find<User>(userToken.UserId);
                    users.DisplayName = name;
                });

                return new ApiResponse {
                    Message = "Your display name has been updated."
                };

            case "avatar" or "banner":
                if (req.ContentType == null) {
                    return new ApiResponse {
                        Status = HttpStatusCode.BadRequest,
                        Message = ResponseStrings.MissingHeader("Content-Type")
                    };
                }

                var stream = new MemoryStream();
                req.InputStream.CopyTo(stream);
        
                // limit to 4MB
                if (stream.Length > 4194304) {
                    return new ApiResponse {
                        Message = "Image too large",
                        Status = HttpStatusCode.BadRequest
                    };
                }
        
                var buffer = StreamUtils.GetPostFile(req.ContentEncoding, req.ContentType, stream);

                if (!buffer.IsImage()) {
                    return new ApiResponse {
                        Message = "Invalid image",
                        Status = HttpStatusCode.BadRequest
                    };
                }
                
                switch (action) {
                    case "avatar":
                        Assets.WriteAsset(AssetType.Avatar, userToken.UserId, buffer);
                
                        return new ApiResponse {
                            Message = "Your avatar has been updated."
                        };
                    case "banner":
                        Assets.WriteAsset(AssetType.Banner, userToken.UserId, buffer);
                
                        return new ApiResponse {
                            Message = "Your banner has been updated."
                        };
                }

                break;
        }
        
        return new ApiResponse {
            Message = "Invalid action",
            Status = HttpStatusCode.BadRequest
        };
    }
}