using System.Net;

namespace fluxel.API.Components;

public interface IApiRoute {
    public string Path { get; }
    public string Method { get; }
    public ApiResponse? Handle(HttpListenerRequest req, HttpListenerResponse res, Dictionary<string, string> parameters);
}
