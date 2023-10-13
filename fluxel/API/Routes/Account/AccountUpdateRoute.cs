using System.Net;
using fluxel.API.Components;
using fluxel.Components.Users;
using fluxel.Constants;
using fluxel.Database.Helpers;
using fluxel.Utils;
using Newtonsoft.Json;

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

        var user = UserHelper.Get(userToken.Id);

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
                if (!name.ValidDisplayName() && name.Length != 0) {
                    return new ApiResponse {
                        Message = "Invalid display name",
                        Status = HttpStatusCode.BadRequest
                    };
                }

                user.DisplayName = name;
                UserHelper.Update(user);

                return new ApiResponse {
                    Message = "Your display name has been updated."
                };

            case "aboutme":
                var aboutme = new StreamReader(req.InputStream).ReadToEnd();

                user.AboutMe = aboutme;
                UserHelper.Update(user);

                return new ApiResponse {
                    Message = "Your about me has been updated."
                };

            case "socials":
                var json = new StreamReader(req.InputStream).ReadToEnd();
                var socials = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

                if (socials == null) {
                    return new ApiResponse {
                        Message = ResponseStrings.InvalidBodyJson,
                        Status = HttpStatusCode.BadRequest
                    };
                }

                foreach (var (key, value) in socials) {
                    switch (key.ToLower()) {
                        case "twitter":
                            user.Socials.Twitter = value;
                            break;

                        case "youtube":
                            user.Socials.YouTube = value;
                            break;

                        case "twitch":
                            user.Socials.Twitch = value;
                            break;

                        case "discord":
                            user.Socials.Discord = value;
                            break;
                    }
                }

                UserHelper.Update(user);

                return new ApiResponse {
                    Message = "Your socials have been updated."
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
                        Assets.WriteAsset(AssetType.Avatar, userToken.Id, buffer);

                        return new ApiResponse {
                            Message = "Your avatar has been updated."
                        };

                    case "banner":
                        Assets.WriteAsset(AssetType.Banner, userToken.Id, buffer);

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
