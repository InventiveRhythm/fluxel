using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace fluxel.Models.Scores;

public class ClubScore
{
    [BsonId]
    public ObjectId ID { get; init; } = ObjectId.GenerateNewId();

    [BsonElement("club")]
    public long ClubID { get; init; }

    [BsonElement("map")]
    public long MapID { get; init; }

    [BsonElement("score")]
    public long TotalScore { get; set; }

    [BsonElement("pr")]
    public double PerformanceRating { get; set; }

    [BsonElement("accuracy")]
    public double Accuracy { get; set; }
}
