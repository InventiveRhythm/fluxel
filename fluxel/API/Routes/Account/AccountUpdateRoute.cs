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
        
        if (req.ContentType == null) {
            return new ApiResponse {
                Status = 400,
                Message = "Missing content type"
            };
        }
        
        var enc = req.ContentEncoding;
        var boundaryBytes = enc.GetBytes(GetBoundary(req.ContentType));
        var boundaryLen = boundaryBytes.Length;
        
        var input = req.InputStream;
        var buffer = new byte[4194304];
        var len = input.Read(buffer, 0, buffer.Length);
        int startPos;
        
        // limit to 4MB
        if (len > 4194304) {
            return new ApiResponse {
                Message = "Image too large",
                Status = 400
            };
        }
        
        // Find start boundary
        while (true)
        {
            if (len == 0)
                throw new Exception("Start Boundaray Not Found");

            startPos = IndexOf(buffer, len, boundaryBytes);
            if (startPos >= 0)
                break;

            Array.Copy(buffer, len - boundaryLen, buffer, 0, boundaryLen);
            len = input.Read(buffer, boundaryLen, 1024 - boundaryLen);
        }
        
        for (var i = 0; i < 4; i++)
        {
            while (true)
            {
                if (len == 0) 
                    throw new Exception("Preamble not Found.");

                startPos = Array.IndexOf(buffer, enc.GetBytes("\n")[0], startPos);
                if (startPos >= 0)
                {
                    startPos++;
                    break;
                }

                len = input.Read(buffer, 0, buffer.Length);
            }
        }
        
        Array.Copy(buffer, startPos, buffer, 0, len - startPos);
        
        // remove last null bytes
        buffer = buffer.Take(len - startPos).ToArray();
        
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
        
        return new ApiResponse {
            Message = "Invalid action",
            Status = 400
        };
    }
    
    private static string GetBoundary(string ctype)
    {
        return "--" + ctype.Split(';')[1].Split('=')[1];
    }
    
    private static int IndexOf(IReadOnlyList<byte> buffer, int len, IReadOnlyList<byte> boundaryBytes)
    {
        for (var i = 0; i <= len - boundaryBytes.Count; i++)
        {
            var match = true;
            for (var j = 0; j < boundaryBytes.Count && match; j++)
                match = buffer[i + j] == boundaryBytes[j];

            if (match)
                return i;
        }

        return -1;
    }
}