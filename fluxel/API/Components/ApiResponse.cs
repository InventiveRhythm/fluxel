using Newtonsoft.Json;

namespace fluxel.API.Components; 

public class ApiResponse {
    [JsonProperty("status")]
    public int Status { get; set; } = 200;
    
    [JsonProperty("message")]
    public string Message { get; set; } = "OK";
    
    [JsonProperty("data")]
    public object Data { get; set; } = new();
}