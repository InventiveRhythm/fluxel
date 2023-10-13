using Newtonsoft.Json;

namespace fluxel.Components.Users;

public class UserShort {
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("username")]
    public string Username { get; set; } = "";

    [JsonProperty("displayname")]
    public string DisplayName { get; set; } = "";

    [JsonProperty("country")]
    public string? CountryCode { get; set; } = "";

    [JsonProperty("role")]
    public int Role { get; set; }

    [JsonProperty("social")]
    public UserSocials Socials { get; set; } = new();
}
