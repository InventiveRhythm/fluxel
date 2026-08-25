using MongoDB.Bson.Serialization.Attributes;

namespace fluxel.Database.Migrations;

public class MigrationCompletion
{
    [BsonId]
    public string ID { get; set; } = string.Empty;

    [BsonElement("version")]
    public long Version { get; set; }
}
