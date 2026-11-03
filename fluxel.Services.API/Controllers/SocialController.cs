using System;
using System.Collections.Generic;
using System.Linq;
using fluxel.Components;
using fluxel.Models;
using fluxel.Models.Users;
using fluxel.Models.Users.Flags;
using fluXis.Online.API.Models.Multi;
using fluXis.Online.API.Models.Notifications;
using fluXis.Online.API.Models.Social;
using Microsoft.EntityFrameworkCore;
using Midori.API.Attributes;
using Midori.API.Components;

namespace fluxel.Services.API.Controllers;

[Controller("/social")]
public class SocialController
{
    private readonly DatabaseContext database;
    private readonly ModelTranslator translator;

    public SocialController(DatabaseContext database, ModelTranslator translator)
    {
        this.database = database;
        this.translator = translator;
    }

    [Authenticated]
    [HttpRoute("/friends")]
    public APIReturn<APIFriends> Friends(User auth)
    {
        var following = database.UserFollows
                                .Include(x => x.Followee)
                                .Where(x => x.FollowerID == auth.ID)
                                .ToArray();

        var lobbies = new List<MultiplayerRoom>();

        // TODO: put multi lobbies in valkey
        // if (multi != null)
        // lobbies = following.Select(multi.WithPlayer).OfType<MultiplayerRoom>().ToList();

        return new APIFriends
        {
            Users = [.. following.Select(x => translator.ToAPI(x.Followee, auth.ID, include: UserIncludes.LastLogin | UserIncludes.Following))],
            Rooms = lobbies
        };
    }

    [Authenticated]
    [HttpRoute("/notifications")]
    public APIReturn<APINotificationList> Notifications(User auth)
    {
        var notifications = database.Notifications.Where(x => x.UserID == auth.ID);

        return new APINotificationList
        {
            Notifications = [.. notifications.Select(x => translator.ToAPI(x)).OfType<APINotification>()],
            LastRead = new DateTimeOffset(auth.LastNotificationRead ?? DateTime.MinValue).ToUnixTimeSeconds()
        };
    }
}
