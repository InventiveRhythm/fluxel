using System.Net;
using fluxel.API.Components;
using fluxel.Database.Helpers;

namespace fluxel.API.Routes;

public class StatsRoute : IApiRoute {
    public string Path => "/stats";
    public string Method => "GET";

    public ApiResponse Handle(HttpListenerRequest req, HttpListenerResponse res, Dictionary<string, string> parameters) {
        return new ApiResponse {
            Data = new {
                users = UserHelper.Count - 1,
                online = GlobalStatistics.Online,
                scores = ScoreHelper.Count,
                mapsets = MapSetHelper.Count
            }
        };
    }
}
