using System.Net;
using fluxel.API.Components;
using fluxel.Database.Helpers;

namespace fluxel.API.Routes.Maps;

public class MapSetsRoute : IApiRoute
{
    public string Path => "/mapsets";
    public string Method => "GET";

    public ApiResponse Handle(HttpListenerRequest req, HttpListenerResponse res, Dictionary<string, string> parameters)
    {
        return new ApiResponse
        {
            Data = MapSetHelper.All
        };
    }
}
