using System.Net;
using System.Reflection;
using System.Text;
using fluxel.API.Components;
using Newtonsoft.Json;

namespace fluxel.API; 

public class ApiServer {
    private static readonly Dictionary<string, IApiRoute> Routes = new();
    private static HttpListener? _listener;
    private static bool _running = true;
    
    public static void Start() {
        LoadRoutes();
        
        _listener = new HttpListener();
        _listener.Prefixes.Add("http://localhost:2434/");
        _listener.Start();
        
        Task.Run(async () => {
            while (_running) {
                await Handle();
            }
        });
        
        Console.WriteLine("Started API server on port 2434.");
    }

    private static void LoadRoutes() {
        Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.GetInterfaces().Contains(typeof(IApiRoute)))
            .ToList()
            .ForEach(t => {
                var route = (IApiRoute) Activator.CreateInstance(t)!;
                Console.WriteLine($"Found route {route.Method} {route.Path}");
                Routes.Add(route.Path, route);
            });
    }

    private static async Task Handle() {
        var context = await _listener?.GetContextAsync()!;
        
        var req = context.Request;
        var res = context.Response;
        
        Console.WriteLine($"{req.HttpMethod} {req.Url?.AbsolutePath}");
        
        IApiRoute? route = null;
        Dictionary<string, string> parameters = new();

        foreach (var (path, handler) in Routes) {
            if (req.HttpMethod != handler.Method) continue; // don't even bother if the method doesn't match
            if (req.Url == null) continue; // don't even know how this would happen but ok

            var parts = path.Split('/');
            var reqParts = req.Url.AbsolutePath.Split('/');
            
            if (parts.Length == 0 || reqParts.Length == 0) continue;
            if (reqParts.Last() == "") reqParts = reqParts[..^1]; // remove trailing slash (if any)
            if (parts.Length != reqParts.Length) continue;
            
            var match = true;
            Dictionary<string, string> reqParams = new();

            for (var i = 0; i < parts.Length; i++) {
                if (parts[i].StartsWith(":")) {
                    reqParams.Add(parts[i].Substring(1), reqParts[i]);
                } else if (!parts[i].Equals(reqParts[i])) {
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
                Status = 404,
                Message = "Not Found"
            };
        } else {
            try {
                response = route.Handle(req, res, parameters);
            }
            catch (Exception e) {
                response = new ApiResponse {
                    Status = 500,
                    Message = "Internal Server Error",
                    Data = e.Message
                };
                Console.WriteLine(e);
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