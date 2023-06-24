using System.Net;
using fluxel.API.Components;
using fluxel.Components.Scores;
using fluxel.Database;

namespace fluxel.API.Routes.Scores; 

public class ScoreRoute : IApiRoute {
    public string Path => "/scores/id/:id";
    public string Method => "GET";
    
    public ApiResponse Handle(HttpListenerRequest req, HttpListenerResponse res, Dictionary<string, string> parameters) {
        if (!int.TryParse(parameters["id"], out var id)) {
            return new ApiResponse {
                Status = 400,
                Message = "Invalid Score ID"
            };
        }

        return RealmAccess.Run(realm => {
            var score = realm.Find<Score>(id);
            
            if (score == null) {
                return new ApiResponse {
                    Status = 404,
                    Message = "Score not found"
                };
            }
            
            return new ApiResponse {
                Data = score
            };
        });
    }
}