using System.Net;
using fluxel.API.Components;
using fluxel.Components.Scores;
using fluxel.Components.Users;
using fluxel.Constants;
using fluxel.Database.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace fluxel.API.Routes.Scores;

public class ScoreUploadRoute : IApiRoute {
    public string Path => "/scores/upload";
    public string Method => "POST";

    public ApiResponse Handle(HttpListenerRequest req, HttpListenerResponse res, Dictionary<string, string> parameters) {
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
                Status = HttpStatusCode.NotFound,
                Message = ResponseStrings.TokenUserNotFound
            };
        }

        var input = new StreamReader(req.InputStream).ReadToEnd();
        var scoreJson = JsonConvert.DeserializeObject<JObject>(input);

        if (scoreJson == null) {
            return new ApiResponse {
                Status = HttpStatusCode.BadRequest,
                Message = ResponseStrings.InvalidBodyJson
            };
        }

        var hash = scoreJson.Value<string>("hash");
        var mods = scoreJson.Value<JArray>("mods")?.Select(t => t.Value<string>()) ?? new List<string>();
        var scrollSpeed = scoreJson.Value<float>("scrollSpeed");
        var maxCombo = scoreJson.Value<int>("maxCombo");
        var flawlessCount = scoreJson.Value<int>("flawless");
        var perfectCount = scoreJson.Value<int>("perfect");
        var greatCount = scoreJson.Value<int>("great");
        var alrightCount = scoreJson.Value<int>("alright");
        var okayCount = scoreJson.Value<int>("okay");
        var missCount = scoreJson.Value<int>("miss");

        if (hash == null) {
            return new ApiResponse {
                Status = HttpStatusCode.BadRequest,
                Message = ResponseStrings.InvalidBodyMissingProperty("hash")
            };
        }

        var map = MapHelper.GetByHash(hash);

        if (map == null) {
            return new ApiResponse {
                Status = HttpStatusCode.NotFound,
                Message = "This map is not uploaded to the server!"
            };
        }

        var mapset = MapSetHelper.Get(map.SetId);

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

        var score = new Score {
            Id = ScoreHelper.NextId,
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

        ScoreHelper.Add(score);

        /*return RealmAccess.Run(realm => {


            realm.Add(score);

            /*if (mapset.Status == 3) {
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
            };#1#


        });*/

        return new ApiResponse {
            Data = new {
                ovr = user.OverallRating,
                ptr = user.PotentialRating,
                ovrChange = user.OverallRating - ovr,
                ptrChange = user.PotentialRating - ptr
            }
        };
    }
}
