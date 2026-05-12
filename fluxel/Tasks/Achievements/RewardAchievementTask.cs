using System;
using System.Threading.Tasks;
using fluxel.Constants.Achievements;
using fluxel.Database;
using fluxel.Modules;
using fluxel.Modules.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace fluxel.Tasks.Achievements;

public class RewardAchievementTask : IBasicTask
{
    public string Name => $"RewardAchievement({uid}, {aid})";

    private long uid { get; }
    private string aid { get; }

    /// <param name="uid">user id</param>
    /// <param name="aid">achievement id</param>
    public RewardAchievementTask(long uid, string aid)
    {
        this.uid = uid;
        this.aid = aid;
    }

    public Task Run(IServiceProvider services)
    {
        var modules = services.GetRequiredService<ModuleManager>();
        var achievements = services.GetRequiredService<AchievementManager>();

        var achievement = AchievementList.Find(aid);

        if (achievement == null || achievements.HasRewarded(aid, uid))
            return Task.CompletedTask;

        modules.SendMessage(new UserAchievementMessage(uid, achievement));
        achievements.Reward(aid, uid);
        return Task.CompletedTask;
    }
}
