using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace fluxel.Models.Auth;

public class AuthMethod
{
    [BsonId]
    public ObjectId ID { get; set; } = ObjectId.GenerateNewId();

    [BsonElement("user")]
    public long UserID { get; set; }

    [BsonElement("method")]
    public string Method { get; set; } = string.Empty;

    [BsonElement("data")]
    public BsonDocument Data { get; set; } = new();
}
