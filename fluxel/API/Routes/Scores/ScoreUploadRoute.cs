using System.Net;
using fluxel.API.Components;
using fluxel.Components.Maps;
using fluxel.Components.Scores;
using fluxel.Components.Users;
using fluxel.Constants;
using fluxel.Database;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace fluxel.API.Routes.Scores;

public class ScoreUploadRoute : IApiRoute {
    public string Path => "/scores/upload";
    public string Method => "POST";

    public ApiResponse? Handle(HttpListenerRequest req, HttpListenerResponse res, Dictionary<string, string> parameters) {
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
                Status = HttpStatusCode.NotFound,
                Message = ResponseStrings.TokenUserNotFound
            };
        }

        var input = new StreamReader(req.InputStream).ReadToEnd();
        var score = JsonConvert.DeserializeObject<JObject>(input);

        if (score == null) {
            return new ApiResponse {
                Status = HttpStatusCode.BadRequest,
                Message = ResponseStrings.InvalidBodyJson
            };
        }

        var hash = score.Value<string>("hash");
        var mods = score.Value<JArray>("mods")?.Select(t => t.Value<string>()) ?? new List<string>();
        var scrollSpeed = score.Value<float>("scrollSpeed");
        var maxCombo = score.Value<int>("maxCombo");
        var flawlessCount = score.Value<int>("flawless");
        var perfectCount = score.Value<int>("perfect");
        var greatCount = score.Value<int>("great");
        var alrightCount = score.Value<int>("alright");
        var okayCount = score.Value<int>("okay");
        var missCount = score.Value<int>("miss");

        if (hash == null) {
            return new ApiResponse {
                Status = HttpStatusCode.BadRequest,
                Message = ResponseStrings.InvalidBodyMissingProperty("hash")
            };
        }

        var map = Map.GetByHash(hash);

        if (map == null) {
            return new ApiResponse {
                Status = HttpStatusCode.NotFound,
                Message = "This map is not uploaded to the server!"
            };
        }

        var mapset = MapSet.FindById(map.SetId);

        if (mapset == null) {
            return new ApiResponse {
                Status = HttpStatusCode.NotFound,
                Message = ResponseStrings.MapSetNotFound
            };
        }

        var mapid = map.Id;
        var userid = user.Id;
        var ovr = user.OverallRating;
        var ptr = user.PotentialRating;

        return RealmAccess.Run(realm => {
            var scoreObj = new Score {
                Id = Score.GetNextId(),
                UserId = userid,
                MapId = mapid,
                Time = DateTimeOffset.Now,
                Mods = string.Join(",", mods),
                MaxCombo = maxCombo,
                FlawlessCount = flawlessCount,
                PerfectCount = perfectCount,
                GreatCount = greatCount,
                AlrightCount = alrightCount,
                OkayCount = okayCount,
                MissCount = missCount,
                ScrollSpeed = scrollSpeed
            };

            realm.Add(scoreObj);

            user = realm.Find<User>(userid);

            if (mapset.Status == 3) {
                // TODO: Calculate rank
                return new ApiResponse {
                    Data = new {
                        ovr = user.OverallRating,
                        ptr = user.PotentialRating,
                        ovrChange = user.OverallRating - ovr,
                        ptrChange = user.PotentialRating - ptr
                    }
                };
            }

            return new ApiResponse {
                Status = HttpStatusCode.BadRequest,
                Message = "This map is not ranked!"
            };
        });
    }
}
