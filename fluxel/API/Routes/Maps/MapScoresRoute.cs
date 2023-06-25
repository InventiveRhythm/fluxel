using System.Net;
using fluxel.API.Components;
using fluxel.Components.Maps;
using fluxel.Components.Scores;
using fluxel.Database;

namespace fluxel.API.Routes.Maps; 

public class MapScoresRoute : IApiRoute {
    public string Path => "/map/:id/scores";
    public string Method => "GET";
    
    public ApiResponse? Handle(HttpListenerRequest req, HttpListenerResponse res, Dictionary<string, string> parameters) {
        if (!int.TryParse(parameters["id"], out var id)) {
            return new ApiResponse {
                Status = 400,
                Message = "Invalid Map ID"
            };
        }

        return RealmAccess.Run(realm => {
            var map = realm.Find<Map>(id);
            
            if (map == null) {
                return new ApiResponse {
                    Status = 404,
                    Message = "Map not found"
                };
            }
            
            var all = realm.All<Score>().Where(s => s.MapId == map.Id).ToList().OrderByDescending(s => s.TotalScore).ToList();
            
            var scores = new List<Score>();
            
            foreach (var score in all) {
                if (scores.Count >= 50)
                    break;

                if (scores.Any(s => s.UserId == score.UserId))
                    continue;

                scores.Add(score);
            }

            return new ApiResponse {
                Data = new {
                    scores,
                    map = map.ToShort()
                }
            };
        });
    }
}