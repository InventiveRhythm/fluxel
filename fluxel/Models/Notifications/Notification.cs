using System;
using fluXis.Online.API.Models.Notifications;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace fluxel.Models.Notifications;

public class Notification
{
    [BsonId]
    public ObjectId ID { get; set; } = ObjectId.GenerateNewId();

    [BsonElement("user")]
    public long UserID { get; set; }

    [BsonElement("type")]
    public NotificationType Type { get; set; }

    [BsonElement("time")]
    public DateTime Time { get; set; } = DateTime.UtcNow;

    #region Extra Data

    [BsonElement("club-invite-code")]
    public string? ClubInviteCode { get; set; }

    #endregion

    public Notification(long id, NotificationType type)
    {
        UserID = id;
        Type = type;
    }

    [BsonConstructor]
    public Notification()
    {
    }
}
