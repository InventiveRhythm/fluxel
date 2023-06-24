using System.Net;
using fluxel.API.Components;
using fluxel.Components.Maps;
using fluxel.Database;

namespace fluxel.API.Routes.Maps; 

public class MapRoute : IApiRoute {
    public string Path => "/map/:id";
    public string Method => "GET";
    
    public ApiResponse Handle(HttpListenerRequest req, HttpListenerResponse res, Dictionary<string, string> parameters) {
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
            
            return new ApiResponse {
                Data = map
            };
            
            // bpom 148
            // not 1857
            // l 273
            // len 156137
        });
    }
}