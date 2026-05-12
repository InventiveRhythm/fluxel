using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace fluxel.Models.Chat;

[JsonObject(MemberSerialization.OptIn)]
public class ChatMessage
{
    [BsonId]
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid ID { get; set; } = Guid.NewGuid();

    [BsonElement("discord")]
    public ulong? DiscordID { get; set; }

    [BsonElement("created")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

    [BsonElement("sender")]
    public long SenderID { get; init; }

    [BsonElement("content")]
    public string Content { get; init; } = string.Empty;

    [BsonElement("channel")]
    public string Channel { get; init; } = string.Empty;

    [BsonElement("deleted")]
    public bool Deleted { get; set; }
}
