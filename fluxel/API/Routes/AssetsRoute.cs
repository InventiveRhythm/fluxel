using System.Net;
using System.Text;
using fluxel.API.Components;

namespace fluxel.API.Routes;

public class AssetsRoute : IApiRoute {
    public string Path => "/assets/:type/:id";
    public string Method => "GET";

    public ApiResponse? Handle(HttpListenerRequest req, HttpListenerResponse res, Dictionary<string, string> parameters) {
        var type = parameters["type"];
        var id = parameters["id"];

        if (!Enum.TryParse<AssetType>(type, true, out var assetType)) {
            return new ApiResponse {
                Status = HttpStatusCode.BadRequest,
                Message = $"'{type}' is not a valid asset type."
            };
        }

        if (id.Contains('.'))
            id = id.Split('.')[0];

        var asset = Assets.GetAsset(assetType, id);
        res.ContentLength64 = asset.Length;
        res.ContentType = "image/png";
        res.ContentEncoding = Encoding.UTF8;
        res.AddHeader("Content-Type", "image/png");
        res.AddHeader("Access-Control-Allow-Origin", "*");
        res.AddHeader("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
        res.AddHeader("Access-Control-Allow-Headers", "*");

        Task.Run(async () => {
            await res.OutputStream.WriteAsync(asset);
            res.Close();
        });

        return null;
    }
}
