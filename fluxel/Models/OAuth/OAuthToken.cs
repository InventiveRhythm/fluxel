using System;
using MongoDB.Bson.Serialization.Attributes;

namespace fluxel.Models.OAuth;

public class OAuthToken
{
    [BsonId]
    public string AccessToken { get; init; } = string.Empty;

    [BsonElement("user")]
    public long UserID { get; init; }

    [BsonElement("scopes")]
    public OAuthScopes[] Scopes { get; init; } = Array.Empty<OAuthScopes>();

    [BsonElement("expire")]
    public long ExpireTime { get; init; }
}
