using Newtonsoft.Json;

namespace fluxel.Components.Users; 

public class UserShort {
    [JsonProperty("id")]
    public int Id { get; set; }
    
    [JsonProperty("username")]
    public string Username { get; set; } = "";
}