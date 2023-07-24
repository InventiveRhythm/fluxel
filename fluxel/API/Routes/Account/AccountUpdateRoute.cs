using System.Net;
using fluxel.API.Components;
using fluxel.Components.Users;
using fluxel.Constants;
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
        
        var user = UserToken.GetByToken(token);
        
        if (user == null) {
            return new ApiResponse {
                Status = HttpStatusCode.Unauthorized,
                Message = ResponseStrings.InvalidToken
            };
        }

        switch (action) {
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
                        Assets.WriteAsset(AssetType.Avatar, user.UserId, buffer);
                
                        return new ApiResponse {
                            Message = "Your avatar has been updated."
                        };
                    case "banner":
                        Assets.WriteAsset(AssetType.Banner, user.UserId, buffer);
                
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