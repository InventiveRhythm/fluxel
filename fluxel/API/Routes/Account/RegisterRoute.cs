using System.Net;
using fluxel.API.Components;
using fluxel.API.Utils;
using fluxel.Components.Users;
using fluxel.Database;
using Newtonsoft.Json;

namespace fluxel.API.Routes.Account;

public class RegisterRoute : IApiRoute {
    public string Path => "/account/register";
    public string Method => "POST";

    public ApiResponse Handle(HttpListenerRequest req, HttpListenerResponse res, Dictionary<string, string> parameters) {
        return new ApiResponse {
            Message = "This route is deprecated. Register is now handled by the Websocket.",
            Status = HttpStatusCode.BadRequest
        };
    }
}
