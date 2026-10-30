using System;
using System.Collections.Generic;
using System.Linq;
using fluxel.Components;
using fluxel.Constants;
using fluxel.Database.Extensions;
using fluxel.Models;
using fluxel.Models.Users;
using fluxel.Models.Users.Flags;
using fluxel.Utils;
using fluXis.Online.API.Models.Users;
using fluXis.Online.API.Payloads.Users;
using fluXis.Utils;
using Microsoft.EntityFrameworkCore;
using Midori.API.Attributes;
using Midori.API.Components;
using Midori.Networking;
using rhym;

namespace fluxel.Services.API.Controllers.Users;

[Controller("/users/:id")]
public class SingleUserController
{
    private readonly DatabaseContext database;
    private readonly ModelTranslator translator;

    public SingleUserController(DatabaseContext database, ModelTranslator translator)
    {
        this.database = database;
        this.translator = translator;
    }

    [Authenticated(Required = false)]
    [HttpRoute("/")]
    public APIReturn<APIUser> User(User? auth, long id, [Source(ParameterSource.Query)] string? mode)
    {
        if (!database.Users.TryFind(out var user, id))
            return Returns.NotFound();

        var includes = UserIncludes.CreatedAt
                       | UserIncludes.LastLogin
                       | UserIncludes.Statistics;

        if (auth != null)
            includes |= UserIncludes.Following;

        ResourceLocation? rl = null;
        if (!string.IsNullOrWhiteSpace(mode)) rl = mode;
        return translator.ToAPI(user, auth?.ID ?? -1, rl, includes);
    }

    [Authenticated]
    [HttpRoute("/", APIMethod.Patch)]
    public APIReturn<APIUser> EditUser(User auth, long id, [Source(ParameterSource.Body)] UserProfileUpdatePayload payload)
    {
        if (!database.Users.TryFind(out var user, id))
            return Returns.NotFound("user");
        if (user.ID != auth.ID)
            return Returns.Message(HttpStatusCode.Forbidden, ResponseStrings.NoPermission);

        database.Entry(user).Reference(x => x.Profile).Load();

        #region Validations

        (string? hash, bool animated) avatar = (user.Profile?.AvatarHash, user.Profile?.HasAnimatedAvatar ?? false);
        (string? hash, bool animated) banner = (user.Profile?.BannerHash, user.Profile?.HasAnimatedBanner ?? false);

        // TODO: move uploads to content and ask for content IDs
        if (!string.IsNullOrEmpty(payload.Avatar))
        {
            var bytes = Convert.FromBase64String(payload.Avatar);

            if (bytes.Length > Assets.MAX_IMAGE_SIZE)
                return Returns.Message(HttpStatusCode.BadRequest, "Avatar is bigger than 3MB.");
            if (!bytes.IsImage())
                return Returns.Message(HttpStatusCode.BadRequest, "Avatar is not a valid image.");

            var gif = bytes.IsGif();

            if (gif && !user.IsSupporter)
                return Returns.Message(HttpStatusCode.BadRequest, "You are not allowed to use animated avatars.");

            var hash = gif ? Assets.WriteAnimatedImage(AssetType.Avatar, bytes) : Assets.WriteHashedImage(AssetType.Avatar, bytes);

            if (string.IsNullOrWhiteSpace(hash))
                return Returns.Message(HttpStatusCode.BadRequest, "Failed to update avatar!");

            avatar = (hash, gif);
        }

        if (!string.IsNullOrEmpty(payload.Banner))
        {
            var bytes = Convert.FromBase64String(payload.Banner);

            if (bytes.Length > Assets.MAX_IMAGE_SIZE)
                return Returns.Message(HttpStatusCode.BadRequest, "Banner is bigger than 3MB.");
            if (!bytes.IsImage())
                return Returns.Message(HttpStatusCode.BadRequest, "Banner is not a valid image.");

            var gif = bytes.IsGif();

            if (gif && !user.IsSupporter)
                return Returns.Message(HttpStatusCode.BadRequest, "You are not allowed to use animated banners.");

            var hash = gif ? Assets.WriteAnimatedImage(AssetType.Banner, bytes) : Assets.WriteHashedImage(AssetType.Banner, bytes);

            if (string.IsNullOrWhiteSpace(hash))
                return Returns.Message(HttpStatusCode.BadRequest, "Failed to update banner!");

            banner = (hash, gif);
        }

        #endregion

        using (database.EditAndSave())
        {
            var profile = user.Profile ?? database.UserProfiles.Add(new UserProfile { User = user }).Entity;
            profile.AvatarHash = avatar.hash;
            profile.HasAnimatedAvatar = avatar.animated;
            profile.BannerHash = banner.hash;
            profile.HasAnimatedBanner = banner.animated;

            profile.DisplayName = (payload.DisplayName ?? profile.DisplayName)?.Trim();
            profile.AboutMe = payload.AboutMe ?? profile.AboutMe;
            profile.Pronouns = payload.Pronouns ?? profile.Pronouns;
        }

        return translator.ToAPI(user);
    }

    [HttpRoute("/followers")]
    public APIReturn<List<APIUser>> Followers(long id)
    {
        if (!database.Users.TryFind(out var user, id))
            return Returns.NotFound("user");

        var follows = database.UserFollows
                              .Include(x => x.Follower)
                              .Where(x => x.FolloweeID == id)
                              .ToArray();

        var ids = follows.Select(x => x.FollowerID).ToArray();
        var followers = follows.Select(x => translator.ToAPI(x.Follower!));

        // sort with the most recent followers first
        followers = followers.OrderByDescending(x => ids.IndexOf(x.ID));
        return followers.ToList();
    }

    [Authenticated(Required = false)]
    [HttpRoute("/maps")]
    public APIReturn<APIUserMaps> Maps(User? auth, long id)
    {
        if (!database.Users.TryFind(out var user, id))
            return Returns.NotFound("user");

        // TODO: get and fill out maps
        return new APIUserMaps([], [], []);
    }

    // TODO: move to admin
    /*[Authenticated(Scopes.DEV)]
    [HttpRoute("/recalculate", APIMethod.Post)]
    public APIReturn<object> Recalculate(User auth, long id)
    {
        tasks.Schedule(new RecalculateUserTask(id));
        return Returns.Okay();
    }*/

    [HttpRoute("/scores")]
    public APIReturn<APIUserScores> Scores(long id)
    {
        if (!database.Users.TryFind(out var user, id))
            return Returns.NotFound("user");

        // TODO
        return new APIUserScores([], []);
        /*var includes = new List<ScoreIncludes> { ScoreIncludes.Map };
        var sc = scores.GetByUser(user.ID);

        return new APIUserScores
        (
            user.GetRecentScores(cache, sc).Select(x => translator.ToAPI(x, includes)).ToList(),
            user.GetBestScores(cache, sc).Select(x => translator.ToAPI(x, includes)).ToList()
        );*/
    }

    [Authenticated]
    [HttpRoute("/username", APIMethod.Patch)]
    public APIReturn<object> ChangeUsername(User auth, long id, [Source(ParameterSource.Form)] string username)
    {
        if (auth.ID != id)
            return Returns.Message(HttpStatusCode.Forbidden, "You cannot change other user's usernames.");
        if (auth.ID == id && !auth.ForceNameChange)
            return Returns.Message(HttpStatusCode.PaymentRequired, "No name changes available.");
        if (!username.Matches(Validate.USERNAME))
            return Returns.Message(HttpStatusCode.BadRequest, "Username must be between 3 and 16 characters and can only contain A-Z, a-z, 0-9 and _!");
        if (username.IsBlacklisted())
            return Returns.Message(HttpStatusCode.BadRequest, "This username is not available!");

        using (database.EditAndSave())
        {
            auth.Username = username;
            auth.ForceNameChange = false;
        }

        return Returns.Okay();
    }

    #region Following

    [Authenticated]
    [HttpRoute("/follow", APIMethod.Put)]
    public APIReturn<object> StartFollowing(User auth, long id)
    {
        if (!database.Users.TryFind(out var target, id))
            return Returns.NotFound("user");

        if (target.ID == auth.ID)
            return Returns.Message(HttpStatusCode.BadRequest, "You cannot follow yourself.");
        if (database.UserFollows.Find(auth.ID, id) != null)
            return Returns.Message(HttpStatusCode.BadRequest, "You are already following this user.");

        using (database.EditAndSave())
        {
            database.UserFollows.Add(new UserFollow { FollowerID = auth.ID, FolloweeID = target.ID });

            if (target.ID == 0) // make fluxel follow the user back
                database.UserFollows.Add(new UserFollow { FollowerID = target.ID, FolloweeID = auth.ID });
        }

        return Returns.Okay();
    }

    [Authenticated]
    [HttpRoute("/follow", APIMethod.Delete)]
    public APIReturn<object> StopFollowing(User auth, long id)
    {
        if (!database.Users.TryFind(out var target, id))
            return Returns.NotFound("user");
        if (!database.UserFollows.TryFind(out var follow, auth.ID, id))
            return Returns.Message(HttpStatusCode.BadRequest, "You aren't even following them.");

        using (database.EditAndSave())
        {
            database.Remove(follow);

            if (target.ID == 0) // make fluxel follow the user back
            {
                var fluxel = database.UserFollows.Find(target.ID, auth.ID);
                if (fluxel != null) database.UserFollows.Remove(fluxel);
            }
        }

        return Returns.Okay();
    }

    #endregion
}
