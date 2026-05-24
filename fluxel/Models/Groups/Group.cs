using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace fluxel.Models.Groups;

[JsonObject(MemberSerialization.OptIn)]
public class Group
{
    [BsonId]
    public string ID { get; init; } = "";

    /// <summary>
    /// The full name of the group.
    /// </summary>
    [BsonElement("name")]
    public string Name { get; set; } = "";

    /// <summary>
    /// The 3-letter tag of the group.
    /// </summary>
    [BsonElement("tag")]
    public string Tag { get; set; } = "";

    /// <summary>
    /// The color of the group.
    /// </summary>
    [BsonElement("color")]
    public string Color { get; set; } = "#ffffff";

    /// <summary>
    /// Hides the role from the /users routes.
    /// </summary>
    [BsonElement("hidden")]
    public bool Hidden { get; set; }
}
