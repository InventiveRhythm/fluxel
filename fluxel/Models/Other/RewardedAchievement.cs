using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;

namespace fluxel.Models.Other;

[Index(nameof(AchievementID), nameof(UserID), IsUnique = true)]
[Table("achievements")]
public class RewardedAchievement
{
    [Key, Column("_id")]
    public ObjectId ID { get; init; } = ObjectId.GenerateNewId();

    [Column("achievement"), MaxLength(64)]
    public string AchievementID { get; init; } = null!;

    [Column("user")]
    public long UserID { get; init; }

    [Column("timestamp")]
    public long Timestamp { get; init; }

    public RewardedAchievement(string achievementID, long userID)
    {
        AchievementID = achievementID;
        UserID = userID;
        Timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
    }

    [UsedImplicitly]
    private RewardedAchievement()
    {
    }
}
