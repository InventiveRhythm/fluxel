using fluxel.Components.Users;
using fluxel.Database.Helpers;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace fluxel.Components.Chat;

public class ChatMessage {
    public Guid Id { get; set; } = Guid.NewGuid();

    [BsonIgnore]
    [JsonProperty("id")]
    public string IdString => Id.ToString();

    [JsonIgnore]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

    [BsonIgnore]
    [JsonProperty("created")]
    public long CreatedAtUnix => CreatedAt.ToUnixTimeSeconds();

    [JsonIgnore]
    public long SenderId { get; init; }

    [JsonProperty("content")]
    public string Content { get; set; } = string.Empty;

    [JsonProperty("channel")]
    public string Channel { get; set; } = string.Empty;

    public bool Deleted { get; set; }

    [BsonIgnore]
    [JsonProperty("sender")]
    public UserShort Sender => UserHelper.Get(SenderId)?.ToShort() ?? new UserShort();
}
