using System;
using System.Collections.Generic;
using System.Linq;
using fluxel.Components;
using fluxel.Constants;
using fluxel.Database;
using fluxel.Database.Extensions;
using fluxel.Models.Clubs;
using fluxel.Models.Notifications;
using fluxel.Models.Other;
using fluxel.Models.Users;
using fluxel.Utils;
using fluXis.Database.Maps;
using fluXis.Online.API.Models.Clubs;
using fluXis.Online.API.Models.Notifications;
using fluXis.Online.API.Payloads.Clubs;
using fluXis.Online.API.Payloads.Invites;
using fluXis.Utils;
using Midori.API.Attributes;
using Midori.API.Components;
using Midori.Networking;

namespace fluxel.API.Controllers.Clubs;

[Controller("/clubs")]
public class ClubController
{
    private readonly RequestCache cache;
    private readonly ClubManager clubs;
    private readonly ModelTranslator translator;
    private readonly NotificationManager notifications;

    public ClubController(RequestCache cache, ModelTranslator translator, ClubManager clubs, NotificationManager notifications)
    {
        this.cache = cache;
        this.translator = translator;
        this.clubs = clubs;
        this.notifications = notifications;
    }

    #region All Clubs

    [HttpRoute("/")]
    public APIReturn<List<APIClub>> List() => cache.Clubs.All.Select(x => translator.ToAPI(x)).ToList();

    [Authenticated]
    [HttpRoute("/", APIMethod.Post)]
    public APIReturn<APIClub> Create(User auth, [Source(ParameterSource.Body)] CreateClubPayload payload)
    {
        if (clubs.GetWhereUserIsMember(auth.ID) != null)
            return Returns.Message(HttpStatusCode.BadRequest, "You are already in a club.");

        if (clubs.ByName(payload.Name) != null)
            return Returns.Message(HttpStatusCode.BadRequest, "Club with this name already exists");

        payload.Tag = payload.Tag.ToUpperInvariant();

        if (clubs.ByTag(payload.Tag) != null)
            return Returns.Message(HttpStatusCode.BadRequest, "Club with this tag already exists");

        var iconBytes = Array.Empty<byte>();
        var bannerBytes = Array.Empty<byte>();

        if (!string.IsNullOrEmpty(payload.Icon))
        {
            var bytes = Convert.FromBase64String(payload.Icon);

            if (bytes.Length > Assets.MAX_IMAGE_SIZE)
                return Returns.Message(HttpStatusCode.BadRequest, "Icon is bigger than 3MB.");
            if (!bytes.IsImage())
                return Returns.Message(HttpStatusCode.BadRequest, "Icon is not a valid image.");

            iconBytes = bytes;
        }

        if (!string.IsNullOrEmpty(payload.Banner))
        {
            var bytes = Convert.FromBase64String(payload.Banner);

            if (bytes.Length > Assets.MAX_IMAGE_SIZE)
                return Returns.Message(HttpStatusCode.BadRequest, "Banner is bigger than 3MB.");
            if (!bytes.IsImage())
                return Returns.Message(HttpStatusCode.BadRequest, "Banner is not a valid image.");

            bannerBytes = bytes;
        }

        // create club

        var club = new Club
        {
            Name = payload.Name,
            Tag = payload.Tag,
            JoinType = payload.JoinType,
            OwnerID = auth.ID,
            Colors = new List<GradientColor>
            {
                new() { Color = payload.ColorStart, Position = 0 },
                new() { Color = payload.ColorEnd, Position = 1 }
            },
            Members = new List<long> { auth.ID }
        };

        if (iconBytes.Length != 0)
            club.IconHash = Assets.WriteHashedImage(AssetType.ClubIcon, iconBytes);
        if (bannerBytes.Length != 0)
            club.BannerHash = Assets.WriteHashedImage(AssetType.ClubBanner, bannerBytes);

        clubs.Add(club);
        return translator.ToAPI(club);
    }

    #endregion

    #region Specific Club

    [HttpRoute("/:id")]
    public APIReturn<APIClub> Get(long id)
    {
        var club = clubs.Get(id);

        if (club == null)
            return Returns.NotFound();

        return translator.ToAPI(club, ClubIncludes.Everything);
    }

    [Authenticated]
    [HttpRoute("/:id", APIMethod.Patch)]
    public APIReturn<APIClub> Edit(User auth, long id, [Source(ParameterSource.Body)] EditClubPayload payload)
    {
        var club = clubs.Get(id);
        if (club == null) return Returns.NotFound();

        if (club.OwnerID != auth.ID && !auth.IsModerator())
            return Returns.Message(HttpStatusCode.Forbidden, ResponseStrings.NoPermission);

        if (!string.IsNullOrWhiteSpace(payload.Name))
        {
            if (payload.Name.Length is < 3 or > 24)
                return Returns.Message(HttpStatusCode.BadRequest, "Name has to be between 3 and 24 characters");

            club.Name = payload.Name;
        }

        if (payload.JoinType != null)
        {
            if (!Enum.IsDefined(typeof(ClubJoinType), payload.JoinType))
                return Returns.Message(HttpStatusCode.BadRequest, "Invalid join type.");

            club.JoinType = payload.JoinType.Value;
        }

        if (!string.IsNullOrWhiteSpace(payload.ColorStart))
        {
            if (!payload.ColorStart.Matches(Validate.REGEX_HEX_COLOR))
                return Returns.Message(HttpStatusCode.BadRequest, "Invalid hex color code for 'color-start'.");

            club.Colors.First().Color = payload.ColorStart;
        }

        if (!string.IsNullOrWhiteSpace(payload.ColorEnd))
        {
            if (!payload.ColorEnd.Matches(Validate.REGEX_HEX_COLOR))
                return Returns.Message(HttpStatusCode.BadRequest, "Invalid hex color code for 'color-end'.");

            club.Colors.Last().Color = payload.ColorEnd;
        }

        if (!string.IsNullOrEmpty(payload.Icon))
        {
            var bytes = Convert.FromBase64String(payload.Icon);

            if (bytes.Length > Assets.MAX_IMAGE_SIZE)
                return Returns.Message(HttpStatusCode.BadRequest, "Icon is bigger than 3MB.");
            if (!bytes.IsImage())
                return Returns.Message(HttpStatusCode.BadRequest, "Icon is not a valid image.");

            club.IconHash = Assets.WriteHashedImage(AssetType.ClubIcon, bytes);
        }

        if (!string.IsNullOrEmpty(payload.Banner))
        {
            var bytes = Convert.FromBase64String(payload.Banner);

            if (bytes.Length > Assets.MAX_IMAGE_SIZE)
                return Returns.Message(HttpStatusCode.BadRequest, "Banner is bigger than 3MB.");
            if (!bytes.IsImage())
                return Returns.Message(HttpStatusCode.BadRequest, "Banner is not a valid image.");

            club.BannerHash = Assets.WriteHashedImage(AssetType.ClubBanner, bytes);
        }

        clubs.Update(club);
        return translator.ToAPI(club, ClubIncludes.JoinType);
    }

    [HttpRoute("/:id/claims")]
    public APIReturn<List<dynamic>> GetClaims(long id)
    {
        var club = clubs.Get(id);

        if (club == null)
            return Returns.NotFound();

        return clubs.GetAllClaimed(id).Select(c =>
                    {
                        var score = clubs.GetScore(c.ClubID, c.MapID);
                        var map = cache.Maps.Get(c.MapID);

                        if (score is null || map is null)
                            return null;

                        return new
                        {
                            score = translator.ToAPI(score),
                            map = translator.ToAPI(map)
                        };
                    }).Where(c => c != null && c.map.Status >= (int)MapStatus.Pure)
                    .Cast<dynamic>().ToList();
    }

    [Authenticated]
    [HttpRoute("/:id/invites")]
    public APIReturn<APIClubInvite> CreateInvite(User auth, long id, [Source(ParameterSource.Body)] CreateClubInvitePayload payload)
    {
        if (payload.UserID == null)
            return Returns.Message(HttpStatusCode.BadRequest, ResponseStrings.MissingJsonField<CreateClubInvitePayload>(nameof(CreateClubInvitePayload.UserID)));

        var club = clubs.Get(id);
        if (club is null) return Returns.NotFound();

        if (club.OwnerID != auth.ID)
            return Returns.Message(HttpStatusCode.Forbidden, ResponseStrings.NoPermission);

        var invite = clubs.CreateInvite(club.ID, payload.UserID.Value);

        notifications.Create(new Notification(payload.UserID.Value, NotificationType.ClubInvite)
        {
            ClubInviteCode = invite.InviteCode
        });

        return translator.ToAPI(invite);
    }

    #endregion
}
