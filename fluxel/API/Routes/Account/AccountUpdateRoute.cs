using System.Net;
using fluxel.API.Components;
using fluxel.Components.Users;
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
                Status = 401,
                Message = "Unauthorized (no token)"
            };
        }
        
        var user = UserToken.GetByToken(token);
        
        if (user == null) {
            return new ApiResponse {
                Status = 401,
                Message = "Unauthorized (invalid token)"
            };
        }

        switch (action) {
            case "avatar" or "banner":
                if (req.ContentType == null) {
                    return new ApiResponse {
                        Status = 400,
                        Message = "Missing content type"
                    };
                }

                var stream = new MemoryStream();
                req.InputStream.CopyTo(stream);
        
                // limit to 4MB
                if (stream.Length > 4194304) {
                    return new ApiResponse {
                        Message = "Image too large",
                        Status = 400
                    };
                }
        
                var buffer = StreamUtils.GetPostFile(req.ContentEncoding, req.ContentType, stream);

                if (!buffer.IsImage()) {
                    return new ApiResponse {
                        Message = "Invalid image",
                        Status = 400
                    };
                }
                
                switch (action) {
                    case "avatar":
                        Assets.WriteAsset(AssetType.Avatar, user.UserId, buffer);
                
                        return new ApiResponse {
                            Message = "Avatar updated",
                            Status = 200
                        };
                    case "banner":
                        Assets.WriteAsset(AssetType.Banner, user.UserId, buffer);
                
                        return new ApiResponse {
                            Message = "Banner updated",
                            Status = 200
                        };
                }

                break;
        }
        
        return new ApiResponse {
            Message = "Invalid action",
            Status = 400
        };
    }
}