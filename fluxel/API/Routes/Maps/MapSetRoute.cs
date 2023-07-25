using System.Net;
using fluxel.API.Components;
using fluxel.Components.Maps;
using fluxel.Constants;
using fluxel.Database;

namespace fluxel.API.Routes.Maps;

public class MapSetRoute : IApiRoute {
    public string Path => "/mapset/:id";
    public string Method => "GET";

    public ApiResponse Handle(HttpListenerRequest req, HttpListenerResponse res, Dictionary<string, string> parameters) {
        if (!int.TryParse(parameters["id"], out var id)) {
            return new ApiResponse {
                Status = HttpStatusCode.BadRequest,
                Message = ResponseStrings.InvalidParameter("id", "integer")
            };
        }

        return RealmAccess.Run(realm => {
            var set = realm.Find<MapSet>(id);

            if (set == null) {
                return new ApiResponse {
                    Status = HttpStatusCode.NotFound,
                    Message = ResponseStrings.MapSetNotFound
                };
            }

            return new ApiResponse {
                Data = set
            };
        });
    }
}
