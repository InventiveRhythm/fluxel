using System.Net;
using Newtonsoft.Json;

namespace fluxel.API.Components; 

public class ApiResponse {
    [JsonProperty("status")]
    public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
    
    [JsonProperty("message")]
    public string Message { get; set; } = "OK";
    
    [JsonProperty("data")]
    public object Data { get; set; } = new();
}