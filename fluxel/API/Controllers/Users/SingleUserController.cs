using System;
using System.Collections.Generic;
using System.Linq;
using fluxel.Components;
using fluxel.Constants;
using fluxel.Database;
using fluxel.Database.Extensions;
using fluxel.Models.Scores;
using fluxel.Models.Users;
using fluxel.Tasks;
using fluxel.Tasks.Users;
using fluxel.Utils;
using fluXis.Online.API.Models.Users;
using fluXis.Online.API.Payloads.Users;
using fluXis.Utils;
using Midori.API.Attributes;
using Midori.API.Components;
using Midori.Networking;

namespace fluxel.API.Controllers.Users;

[Controller("/users/:id")]
public class SingleUserController
{
    private readonly UserManager users;
    private readonly RequestCache cache;
    private readonly MapManager maps;
    private readonly ModelTranslator translator;
    private readonly ScoreManager scores;
    private readonly TaskRunner tasks;

    public SingleUserController(RequestCache cache, UserManager users, ModelTranslator translator, MapManager maps, ScoreManager scores, TaskRunner tasks)
    {
        this.cache = cache;
        this.users = users;
        this.translator = translator;
        this.maps = maps;
        this.scores = scores;
        this.tasks = tasks;
    }

    [Authenticated(Required = false)]
    [HttpRoute("/")]
    public APIReturn<APIUser> User(User? auth, long id, [Source(ParameterSource.Query)] int mode = 0)
    {
        if (mode is > 8 or < 4)
            mode = 0;

        if (!users.TryGet(id, out var user))
            return Returns.NotFound();

        var includes = UserIncludes.CreatedAt
                       | UserIncludes.LastLogin
                       | UserIncludes.Socials
                       | UserIncludes.Statistics;

        if (auth != null)
            includes |= UserIncludes.Following;
        if (auth?.ID == id)
            includes |= UserIncludes.Email;

        return translator.ToAPI(user, auth?.ID ?? -1, mode, includes);
    }

    [Authenticated]
    [HttpRoute("/", APIMethod.Patch)]
    public APIReturn<APIUser> EditUser(User auth, long id, [Source(ParameterSource.Body)] UserProfileUpdatePayload payload)
    {
        if (!users.TryGet(id, out var user))
            return Returns.NotFound();
        if (user.ID != auth.ID && !auth.IsModerator())
            return Returns.Message(HttpStatusCode.Forbidden, ResponseStrings.NoPermission);

        #region Validations

        (string? hash, bool animated) avatar = (user.AvatarHash, user.HasAnimatedAvatar);
        (string? hash, bool animated) banner = (user.BannerHash, user.HasAnimatedBanner);

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

        user = users.UpdateLocked(user.ID, u =>
        {
            u.AvatarHash = avatar.hash;
            u.HasAnimatedAvatar = avatar.animated;
            u.BannerHash = banner.hash;
            u.HasAnimatedBanner = banner.animated;

            u.DisplayName = (payload.DisplayName ?? u.DisplayName)?.Trim();
            u.AboutMe = payload.AboutMe ?? u.AboutMe;
            u.Pronouns = payload.Pronouns ?? u.Pronouns;
            u.Socials.Discord = payload.Discord ?? u.Socials.Discord;
            u.Socials.Twitch = payload.Twitch ?? u.Socials.Twitch;
            u.Socials.Twitter = payload.Twitter ?? u.Socials.Twitter;
            u.Socials.YouTube = payload.YouTube ?? u.Socials.YouTube;
        });

        return translator.ToAPI(user, include: UserIncludes.Socials);
    }

    [HttpRoute("/followers")]
    public APIReturn<List<APIUser>> Followers(long id)
    {
        if (!users.TryGet(id, out _))
            return Returns.NotFound();

        var followerIDs = users.GetFollowers(id);
        var followers = users.GetMany(followerIDs).Select(x => translator.ToAPI(x));

        // sort with the most recent followers first
        followers = followers.OrderByDescending(x => followerIDs.IndexOf(x.ID));
        return followers.ToList();
    }

    [Authenticated(Required = false)]
    [HttpRoute("/maps")]
    public APIReturn<User.UserMaps> Maps(User? auth, long id)
    {
        if (!users.TryGet(id, out var user))
            return Returns.NotFound();

        return new User.UserMaps(maps, translator, user, auth);
    }

    [Authenticated(Scopes.DEV)]
    [HttpRoute("/recalculate", APIMethod.Post)]
    public APIReturn<object> Recalculate(User auth, long id)
    {
        tasks.Schedule(new RecalculateUserTask(id));
        return Returns.Okay();
    }

    [HttpRoute("/scores")]
    public APIReturn<APIUserScores> Scores(long id)
    {
        if (!users.TryGet(id, out var user))
            return Returns.NotFound();

        var includes = new List<ScoreIncludes> { ScoreIncludes.Map };
        var sc = scores.GetByUser(user.ID);

        return new APIUserScores
        (
            user.GetRecentScores(cache, sc).Select(x => translator.ToAPI(x, includes)).ToList(),
            user.GetBestScores(cache, sc).Select(x => translator.ToAPI(x, includes)).ToList()
        );
    }

    [Authenticated]
    [HttpRoute("/username", APIMethod.Patch)]
    public APIReturn<object> ChangeUsername(User auth, long id, [Source(ParameterSource.Form)] string username)
    {
        if (auth.ID != id && !auth.IsModerator())
            return Returns.Message(HttpStatusCode.Forbidden, "You cannot change other user's usernames.");
        if (auth.ID == id && !auth.ForceNameChange)
            return Returns.Message(HttpStatusCode.PaymentRequired, "No name changes available.");
        if (!username.Matches(Validate.USERNAME))
            return Returns.Message(HttpStatusCode.BadRequest, "Username must be between 3 and 16 characters and can only contain A-Z, a-z, 0-9 and _!");
        if (username.IsBlacklisted())
            return Returns.Message(HttpStatusCode.BadRequest, "This username is not available!");

        users.UpdateLocked(id, u =>
        {
            u.Username = username;
            u.ForceNameChange = false;
        });

        return Returns.Okay();
    }

    #region Following

    [Authenticated]
    [HttpRoute("/follow", APIMethod.Put)]
    public APIReturn<object> StartFollowing(User auth, long id)
    {
        if (!users.TryGet(id, out var target))
            return Returns.NotFound("user");

        if (target.ID == auth.ID)
            return Returns.Message(HttpStatusCode.BadRequest, "You cannot follow yourself.");
        if (users.IsFollowing(auth.ID, target.ID))
            return Returns.Message(HttpStatusCode.BadRequest, "You are already following this user.");

        users.StartFollow(auth.ID, target.ID);

        if (target.ID == 0) // make fluxel follow the user back
            users.StartFollow(0, auth.ID);

        return Returns.Okay();
    }

    [Authenticated]
    [HttpRoute("/follow", APIMethod.Delete)]
    public APIReturn<object> StopFollowing(User auth, long id)
    {
        if (!users.TryGet(id, out var target))
            return Returns.NotFound("user");
        if (!users.IsFollowing(auth.ID, target.ID))
            return Returns.Message(HttpStatusCode.BadRequest, "You aren't even following them.");

        users.StopFollow(auth.ID, target.ID);

        if (target.ID == 0)
            users.StopFollow(0, auth.ID);

        return Returns.Okay();
    }

    #endregion
}
