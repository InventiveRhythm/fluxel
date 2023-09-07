using System.Net;
using fluxel.API.Components;
using fluxel.Components.Maps;
using fluxel.Constants;
using fluxel.Database;

namespace fluxel.API.Routes.Maps;

public class MapHashRoute : IApiRoute {
    public string Path => "/map/hash/:hash";
    public string Method => "GET";

    public ApiResponse Handle(HttpListenerRequest req, HttpListenerResponse res, Dictionary<string, string> parameters) {
        var hash = parameters["hash"];

        return RealmAccess.Run(realm => {
            var map = realm.All<Map>().FirstOrDefault(m => m.Hash == hash);

            if (map == null) {
                return new ApiResponse {
                    Status = HttpStatusCode.NotFound,
                    Message = ResponseStrings.MapNotFound
                };
            }

            return new ApiResponse {
                Data = map
            };
        });
    }
}
