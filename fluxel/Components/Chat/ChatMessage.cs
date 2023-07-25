using fluxel.Components.Users;
using Newtonsoft.Json;
using Realms;

namespace fluxel.Components.Chat;

public class ChatMessage : RealmObject {
    [PrimaryKey]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Ignored]
    [JsonProperty("id")]
    public string IdString => Id.ToString();

    [JsonIgnore]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

    [Ignored]
    [JsonProperty("created")]
    public long CreatedAtUnix => CreatedAt.ToUnixTimeSeconds();

    [JsonIgnore]
    public int SenderId { get; init; }

    [JsonProperty("content")]
    public string Content { get; set; } = string.Empty;

    [JsonProperty("channel")]
    public string Channel { get; set; } = string.Empty;

    [Ignored]
    [JsonProperty("sender")]
    public UserShort Sender => User.FindById(SenderId)?.ToShort() ?? new UserShort();
}
