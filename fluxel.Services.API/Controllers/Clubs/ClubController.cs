using System;
using System.Collections.Generic;
using System.Linq;
using fluxel.Components;
using fluxel.Constants;
using fluxel.Database.Extensions;
using fluxel.Models;
using fluxel.Models.Clubs;
using fluxel.Models.Notifications;
using fluxel.Models.Users;
using fluxel.Utils;
using fluXis.Database.Maps;
using fluXis.Online.API.Models.Clubs;
using fluXis.Online.API.Payloads.Clubs;
using fluXis.Online.API.Payloads.Invites;
using fluXis.Utils;
using Microsoft.EntityFrameworkCore;
using Midori.API.Attributes;
using Midori.API.Components;
using Midori.Networking;
using Midori.Utils;
using osu.Framework.Extensions.Color4Extensions;

namespace fluxel.Services.API.Controllers.Clubs;

[Controller("/clubs")]
public class ClubController
{
    private readonly DatabaseContext database;
    private readonly ModelTranslator translator;

    public ClubController(ModelTranslator translator, DatabaseContext database)
    {
        this.translator = translator;
        this.database = database;
    }

    #region All Clubs

    [HttpRoute("/")]
    public APIReturn<APIClub[]> List()
        => database.Clubs.ToArray().Select(x => translator.ToAPI(x)).ToArray();

    [Authenticated]
    [HttpRoute("/", APIMethod.Post)]
    public APIReturn<APIClub> Create(User auth, [Source(ParameterSource.Body)] CreateClubPayload payload)
    {
        if (auth.ClubID != null)
            return Returns.Message(HttpStatusCode.BadRequest, "You are already in a club.");

        if (database.Clubs.FirstOrDefault(c => string.Equals(c.Name, payload.Name, StringComparison.CurrentCultureIgnoreCase)) != null)
            return Returns.Message(HttpStatusCode.BadRequest, "Club with this name already exists");

        payload.Tag = payload.Tag.ToUpperInvariant();

        if (database.Clubs.FirstOrDefault(c => c.Tag == payload.Tag) != null)
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
            ColorStart = Color4Extensions.FromHex(payload.ColorStart),
            ColorEnd = Color4Extensions.FromHex(payload.ColorEnd)
        };

        if (iconBytes.Length != 0)
            club.IconHash = Assets.WriteHashedImage(AssetType.ClubIcon, iconBytes);
        if (bannerBytes.Length != 0)
            club.BannerHash = Assets.WriteHashedImage(AssetType.ClubBanner, bannerBytes);

        using (database.EditAndSave())
        {
            database.Clubs.Add(club);
            auth.Club = club;
        }

        return translator.ToAPI(club);
    }

    #endregion

    #region Specific Club

    [HttpRoute("/:id")]
    public APIReturn<APIClub> Get(long id)
    {
        if (!database.Clubs.TryFind(out var club, id))
            return Returns.NotFound("club");

        return translator.ToAPI(club, ClubIncludes.Everything);
    }

    [Authenticated]
    [HttpRoute("/:id", APIMethod.Patch)]
    public APIReturn<APIClub> Edit(User auth, long id, [Source(ParameterSource.Body)] EditClubPayload payload)
    {
        if (!database.Clubs.TryFind(out var club, id))
            return Returns.NotFound("club");

        // TODO: moderator check (or move to admin)
        if (club.OwnerID != auth.ID /* && !auth.IsModerator()*/)
            return Returns.Message(HttpStatusCode.Forbidden, ResponseStrings.NoPermission);

        using (database.EditAndSave())
        {
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

                club.ColorStart = Color4Extensions.FromHex(payload.ColorStart);
            }

            if (!string.IsNullOrWhiteSpace(payload.ColorEnd))
            {
                if (!payload.ColorEnd.Matches(Validate.REGEX_HEX_COLOR))
                    return Returns.Message(HttpStatusCode.BadRequest, "Invalid hex color code for 'color-end'.");

                club.ColorEnd = Color4Extensions.FromHex(payload.ColorEnd);
            }

            // todo: ask for uploaded asset id instead
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
        }

        return translator.ToAPI(club, ClubIncludes.JoinType);
    }

    [HttpRoute("/:id/claims")]
    public APIReturn<List<dynamic>> GetClaims(long id)
    {
        if (!database.Clubs.TryFind(out _, id))
            return Returns.NotFound("club");

        var claims = database.ClubClaims
                             .Include(x => x.Map)
                             .Where(x => x.ClubID == id).ToArray();

        return claims.Select(c =>
                     {
                         var score = database.ClubScores.Find(c.MapID, c.ClubID);

                         if (score is null)
                             return null;

                         return new
                         {
                             score = translator.ToAPI(score),
                             map = translator.ToAPI(c.Map)
                         };
                     })
                     .Where(c => c != null && c.map.Status >= (int)MapStatus.Pure)
                     .Cast<dynamic>().ToList();
    }

    [Authenticated]
    [HttpRoute("/:id/invites", APIMethod.Post)]
    public APIReturn<APIClubInvite> CreateInvite(User auth, long id, [Source(ParameterSource.Body)] CreateClubInvitePayload payload)
    {
        if (payload.UserID == null)
            return Returns.Message(HttpStatusCode.BadRequest, ResponseStrings.MissingJsonField<CreateClubInvitePayload>(nameof(CreateClubInvitePayload.UserID)));

        if (!database.Clubs.TryFind(out var club, id))
            return Returns.NotFound("club");

        if (!database.Users.TryFind(out _, payload.UserID.Value))
            return Returns.NotFound("user");

        if (club.OwnerID != auth.ID)
            return Returns.Message(HttpStatusCode.Forbidden, ResponseStrings.NoPermission);

        ClubInvite invite;

        using (database.EditAndSave())
        {
            invite = database.ClubInvites.Add(new ClubInvite
            {
                InviteCode = RandomizeUtils.GenerateRandomString(7),
                ClubID = club.ID,
                UserID = payload.UserID.Value
            }).Entity;

            database.Notifications.Add(new ClubInviteNotification
            {
                UserID = payload.UserID.Value,
                ClubInviteCode = invite.InviteCode
            });
        }

        return translator.ToAPI(invite);
    }

    #endregion
}
