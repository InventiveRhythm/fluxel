using fluxel.Models.Other;
using Midori.Database;

namespace fluxel.Database;

public class AchievementManager
{
    private readonly IDatabaseTable<RewardedAchievement> rewarded;

    public AchievementManager(IDatabaseProvider db)
    {
        rewarded = db.GetTable<RewardedAchievement>("achievements");
    }

    public void Reward(string achievementID, long userID)
    {
        if (HasRewarded(achievementID, userID))
            return;

        rewarded.Add(new RewardedAchievement(achievementID, userID));
    }

    public bool HasRewarded(string achievementID, long userID)
        => rewarded.Count(m => m.AchievementID == achievementID && m.UserID == userID) > 0;
}
