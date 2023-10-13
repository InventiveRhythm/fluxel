using System.Net;
using System.Text;
using fluxel.API.Components;
using fluxel.Constants;
using fluxel.Database.Helpers;

namespace fluxel.API.Routes.Maps;

public class MapDownloadRoute : IApiRoute {
    public string Path => "/mapset/:id/download";
    public string Method => "GET";

    public ApiResponse? Handle(HttpListenerRequest req, HttpListenerResponse res, Dictionary<string, string> parameters) {
        if (!int.TryParse(parameters["id"], out var id)) {
            return new ApiResponse {
                Message = ResponseStrings.InvalidParameter("id", "integer"),
                Status = HttpStatusCode.BadRequest
            };
        }

        var set = MapSetHelper.Get(id);

        if (set == null) {
            return new ApiResponse {
                Message = ResponseStrings.MapSetNotFound,
                Status = HttpStatusCode.NotFound
            };
        }

        var path = $"{Environment.CurrentDirectory}/Maps/{set.Id}.zip";

        if (!File.Exists(path)) {
            return new ApiResponse {
                Message = ResponseStrings.MapSetNotFound,
                Status = HttpStatusCode.NotFound
            };
        }

        var data = File.ReadAllBytes(path);
        res.ContentLength64 = data.Length;
        res.ContentType = "application/zip";
        res.ContentEncoding = Encoding.UTF8;
        res.AddHeader("Content-Type", "application/zip");
        res.AddHeader("Content-Disposition", $"attachment; filename=\"{set.Id} {set.Artist} - {set.Title}.fms\"");
        res.AddHeader("Access-Control-Allow-Origin", "*");
        res.AddHeader("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
        res.AddHeader("Access-Control-Allow-Headers", "*");

        Task.Run(async () => {
            await res.OutputStream.WriteAsync(data);
            res.Close();
        });

        return null;
    }
}
