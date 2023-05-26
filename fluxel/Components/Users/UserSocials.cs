using Newtonsoft.Json;
using Realms;

namespace fluxel.Components.Users; 

public class UserSocials : RealmObject {
    [JsonProperty("discord")]
    public string Discord { get; set; } = "";
    
    [JsonProperty("twitter")]
    public string Twitter { get; set; } = "";
    
    [JsonProperty("youtube")]
    public string YouTube { get; set; } = "";
    
    [JsonProperty("twitch")]
    public string Twitch { get; set; } = "";
}