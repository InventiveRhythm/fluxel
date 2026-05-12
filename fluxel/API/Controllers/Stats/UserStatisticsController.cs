using System.Linq;
using fluxel.Database;
using Midori.API.Attributes;
using Midori.API.Components;

namespace fluxel.API.Controllers.Stats;

[Controller("/stats/users")]
public class UserStatisticsController
{
    private readonly UserManager users;

    public UserStatisticsController(UserManager users)
    {
        this.users = users;
    }

    [HttpRoute("/creation")]
    public APIReturn<object> Creation() => users.AllUsers.Where(u => u.CreatedAt > 0).Select(u => u.CreatedAt).ToList();

    [HttpRoute("/online")]
    public APIReturn<object> Online() => users.AllLogins.Select(x => new
    {
        time = x.Time,
        state = x.IsOnline
    }).ToList();
}
