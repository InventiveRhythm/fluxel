using System;
using JetBrains.Annotations;
using Midori.Utils;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace fluxel.Models.Payment;

public class RedemptionCode
{
    public const string DONATE_AMOUNT = "DONATE_AMOUNT";

    [BsonId]
    public ObjectId ID { get; set; }

    [BsonElement("code")]
    public string Code { get; set; } = string.Empty;

    [BsonElement("uses-max")]
    public int UsesMax { get; set; }

    [BsonElement("uses-current")]
    public int UsesCurrent { get; set; }

    [BsonElement("metadata")]
    public BsonDocument Metadata { get; set; } = null!;

    public RedemptionCode(BsonDocument meta, int uses = 1, string? code = null)
    {
        ID = ObjectId.GenerateNewId();
        Code = code?.ToUpperInvariant() ?? RandomizeUtils.GenerateRandomString(24, CharacterType.Uppercase);
        UsesMax = uses;
        Metadata = meta;
    }

    [BsonConstructor]
    [Obsolete("This is for BSON parsing only.")]
    [UsedImplicitly]
    public RedemptionCode() { }
}
