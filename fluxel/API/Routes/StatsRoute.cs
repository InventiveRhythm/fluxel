using System.Net;
using fluxel.API.Components;
using fluxel.Components.Maps;
using fluxel.Components.Scores;
using fluxel.Components.Users;

namespace fluxel.API.Routes; 

public class StatsRoute : IApiRoute {
    public string Path => "/stats";
    public string Method => "GET";
    
    public ApiResponse? Handle(HttpListenerRequest req, HttpListenerResponse res, Dictionary<string, string> parameters) {
        return new ApiResponse {
            Data = new {
                users = User.Count() - 1, // dont count fluxel
                online = Stats.Online,
                scores = Score.Count(),
                mapsets = MapSet.Count()
            }
        };
    }
}