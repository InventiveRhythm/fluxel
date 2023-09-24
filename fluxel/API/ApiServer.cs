using System.Net;
using System.Reflection;
using System.Text;
using fluxel.API.Components;
using Newtonsoft.Json;

namespace fluxel.API;

public abstract class ApiServer {
    private static readonly Dictionary<string, IApiRoute> routes = new();
    private static HttpListener? listener;
    private static readonly bool running = true;

    public static void Start() {
        loadRoutes();

        listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:2434/");
        listener.Start();

        Task.Run(async () => {
            while (running) {
                await handle();
            }
        });

        Logger.Log("Started API server on port 2434.");
    }

    private static void loadRoutes() {
        Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.GetInterfaces().Contains(typeof(IApiRoute)))
            .ToList()
            .ForEach(t => {
                var route = (IApiRoute) Activator.CreateInstance(t)!;
                Logger.Log($"Found route {route.Method} {route.Path}");
                routes.Add(route.Path, route);
            });
    }

    private static async Task handle() {
        var context = await listener?.GetContextAsync()!;

        var req = context.Request;
        var res = context.Response;

        IApiRoute? route = null;
        Dictionary<string, string> parameters = new();

        foreach (var (path, handler) in routes) {
            if (req.HttpMethod != handler.Method) continue;
            if (req.Url == null) continue; // don't even know how this would happen but ok

            var url = req.Url.AbsolutePath;

            if (path == url && !path.Contains(':')) {
                route = handler; // exact match with no parameters
                break;
            }

            var parts = path.Split('/');
            var reqParts = url.Split('/');

            if (parts.Length == 0 || reqParts.Length == 0) continue;
            if (reqParts.Last() == "") reqParts = reqParts[..^1]; // remove trailing slash (if any)
            if (parts.Length != reqParts.Length) continue;

            var match = true;
            Dictionary<string, string> reqParams = new();

            for (var i = 0; i < parts.Length; i++) {
                if (parts[i].StartsWith(":")) {
                    reqParams.Add(parts[i][1..], reqParts[i]);
                }
                else if (!parts[i].Equals(reqParts[i])) {
                    match = false;
                    break;
                }
            }

            if (!match) continue;

            route = handler;
            parameters = reqParams;
        }

        ApiResponse? response;

        if (route == null) {
            response = new ApiResponse {
                Status = HttpStatusCode.NotFound,
                Message = "The requested route does not exist."
            };
        }
        else {
            try {
                response = route.Handle(req, res, parameters);
            }
            catch (Exception e) {
                response = new ApiResponse {
                    Status = HttpStatusCode.InternalServerError,
                    Message = "Welp, something went very wrong. It's probably not your fault, but please report this to the developers.",
                    Data = new {}
                };
                Logger.Log(e.Message, LogLevel.Error);
            }
        }

        if (response == null) return; // route handled the response itself

        var buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response));
        res.ContentLength64 = buffer.Length;
        res.ContentType = "application/json";
        res.ContentEncoding = Encoding.UTF8;
        res.AddHeader("Content-Type", "application/json");
        res.AddHeader("Access-Control-Allow-Origin", "*");
        res.AddHeader("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
        res.AddHeader("Access-Control-Allow-Headers", "*");
        await res.OutputStream.WriteAsync(buffer);
        res.Close();
    }
}
