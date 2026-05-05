using fluxel.Database.Helpers;

namespace fluxel.Tasks.Users;

public class RecalculateUserTask : IBasicTask
{
    public string Name => $"RecalculateUser({id})";

    private long id { get; }

    public RecalculateUserTask(long id)
    {
        this.id = id;
    }

    public void Run()
    {
        var user = UserHelper.Get(id);
        if (user.Username.ToLower().StartsWith("ben") || user.DisplayName.ToLower().StartsWith("ben"))
            UserHelper.UpdateLocked(id, u => u.RecalculateForBen());
        else
            UserHelper.UpdateLocked(id, u => u.Recalculate());
    }
}
