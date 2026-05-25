using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace fluxel.Models.OAuth;

public class OAuthApplication
{
    [BsonId]
    public ObjectId ID { get; init; } = ObjectId.GenerateNewId();

    [BsonElement("client-id")]
    public string ClientID { get; set; } = string.Empty;

    [BsonElement("client-secret")]
    public string ClientSecret { get; set; } = string.Empty;

    [BsonElement("redirects")]
    public List<string> Redirects { get; set; } = new();
}
