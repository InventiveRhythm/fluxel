using System.Net;
using fluxel.API.Components;

namespace fluxel.API.Routes; 

public class IndexRoute : IApiRoute {
    public string Path => "/";
    public string Method => "GET";
    public ApiResponse Handle(HttpListenerRequest req, HttpListenerResponse res, Dictionary<string, string> parameters) {
        return new ApiResponse {
            Data = "Welcome to fluxel, the API for fluXis!"
        };
    }
}