using System.Net;
using fluxel.API.Components;
using fluxel.Constants;
using fluxel.Database.Helpers;

namespace fluxel.API.Routes.Maps;

public class MapRoute : IApiRoute {
    public string Path => "/map/:id";
    public string Method => "GET";

    public ApiResponse Handle(HttpListenerRequest req, HttpListenerResponse res, Dictionary<string, string> parameters) {
        if (!int.TryParse(parameters["id"], out var id)) {
            return new ApiResponse {
                Status = HttpStatusCode.BadRequest,
                Message = ResponseStrings.InvalidParameter("id", "integer")
            };
        }

        var map = MapHelper.Get(id);

        if (map == null) {
            return new ApiResponse {
                Status = HttpStatusCode.NotFound,
                Message = ResponseStrings.MapNotFound
            };
        }

        return new ApiResponse {
            Data = map
        };
    }
}
