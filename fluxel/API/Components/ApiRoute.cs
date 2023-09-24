using System.Net;
using JetBrains.Annotations;

namespace fluxel.API.Components;

[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public interface IApiRoute {
    public string Path { get; }
    public string Method { get; }
    public ApiResponse? Handle(HttpListenerRequest req, HttpListenerResponse res, Dictionary<string, string> parameters);
}
