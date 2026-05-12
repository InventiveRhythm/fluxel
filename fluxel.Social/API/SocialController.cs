using fluxel.Components;
using fluxel.Database;
using fluxel.Models.Users;
using fluxel.Modules;
using fluXis.Online.API.Models.Multi;
using fluXis.Online.API.Models.Notifications;
using fluXis.Online.API.Models.Social;
using Midori.API.Attributes;
using Midori.API.Components;

namespace fluxel.Social.API;

[Controller("/social")]
public class SocialController
{
    private readonly UserManager users;
    private readonly NotificationManager notifications;
    private readonly ModelTranslator translator;
    private readonly IMultiRoomManager? multi;

    public SocialController(UserManager users, ModelTranslator translator, NotificationManager notifications, IMultiRoomManager? multi = null)
    {
        this.users = users;
        this.translator = translator;
        this.notifications = notifications;
        this.multi = multi;
    }

    [Authenticated]
    [HttpRoute("/friends")]
    public APIReturn<APIFriends> Friends(User auth)
    {
        var following = users.GetFollowing(auth.ID);

        var friends = following.Select(translator.Cache.Users.Get).OfType<User>();
        var lobbies = new List<MultiplayerRoom>();

        if (multi != null)
            lobbies = following.Select(multi.WithPlayer).OfType<MultiplayerRoom>().ToList();

        return new APIFriends
        {
            Users = friends.Select(x => translator.ToAPI(x, auth.ID, include: UserIncludes.LastLogin | UserIncludes.Following)).ToList(),
            Rooms = lobbies
        };
    }

    [Authenticated]
    [HttpRoute("/notifications")]
    public APIReturn<APINotificationList> Notifications(User auth)
    {
        var nf = notifications.ForUser(auth.ID);

        return new APINotificationList
        {
            Notifications = nf.Select(x => translator.ToAPI(x)).OfType<APINotification>().ToList(),
            LastRead = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
    }
}
