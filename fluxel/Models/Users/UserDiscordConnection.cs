using System;
using MongoDB.Bson.Serialization.Attributes;

namespace fluxel.Models.Users;

public class UserDiscordConnection
{
    [BsonId]
    public long ID { get; init; }

    [BsonElement("token")]
    public string AccessToken { get; set; } = string.Empty;

    [BsonElement("refresh")]
    public string RefreshToken { get; set; } = string.Empty;

    [BsonElement("expire")]
    public DateTimeOffset Expire { get; set; }
}
