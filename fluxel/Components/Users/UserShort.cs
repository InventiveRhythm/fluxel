using Newtonsoft.Json;

namespace fluxel.Components.Users; 

public class UserShort {
    [JsonProperty("id")]
    public int Id { get; set; }
    
    [JsonProperty("username")]
    public string Username { get; set; } = "";
    
    [JsonProperty("country")]
    public string? CountryCode { get; set; } = "";
    
    [JsonProperty("role")]
    public int Role { get; set; }

    [JsonProperty("ovr")]
    public double OverallRating { get; set; }

    [JsonProperty("ptr")]
    public double PotentialRating { get; set; }
}