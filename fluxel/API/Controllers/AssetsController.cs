using System.IO;
using Midori.API.Attributes;
using Midori.API.Components;
using Midori.Networking;

namespace fluxel.API.Controllers;

[Controller("/assets")]
public class AssetsController
{
    [HttpRoute("/:type/:id")]
    [ReturnsMime("application/octet-stream")]
    public APIReturn<Stream> Get(string type, string id)
    {
        if (!Assets.TryGetType(type, out var assetType))
            return Returns.Message(HttpStatusCode.BadRequest, $"'{type}' is not a valid asset type.");

        if (id.Contains('.'))
            id = id.Split('.')[0];

        var asset = Assets.GetAsset(assetType.Value, id);
        return new MemoryStream(asset);
    }
}
