using System;
using System.Collections.Generic;
using System.Linq;
using fluxel.Database;
using fluxel.Database.Extensions;
using fluxel.Models.Chat;
using fluxel.Models.Clubs;
using fluxel.Models.Groups;
using fluxel.Models.Maps;
using fluxel.Models.Notifications;
using fluxel.Models.Scores;
using fluxel.Models.Users;
using fluxel.Models.Users.Equipment;
using fluxel.Modules;
using fluxel.Utils;
using fluXis.Online.API.Models.Chat;
using fluXis.Online.API.Models.Clubs;
using fluXis.Online.API.Models.Groups;
using fluXis.Online.API.Models.Maps;
using fluXis.Online.API.Models.Notifications;
using fluXis.Online.API.Models.Notifications.Data;
using fluXis.Online.API.Models.Other;
using fluXis.Online.API.Models.Scores;
using fluXis.Online.API.Models.Users;
using fluXis.Online.API.Models.Users.Equipment;
using Newtonsoft.Json.Linq;
using osu.Framework.Extensions.EnumExtensions;

namespace fluxel.Components;

/// <summary>
/// Transient class that converts all database models to their respective API variants.
/// </summary>
public class ModelTranslator
{
    private readonly IOnlineStateManager? onlineStates;
    private readonly ClubManager clubs;
    private readonly GroupManager groups;
    private readonly MapManager maps;
    private readonly UserManager users;

    public RequestCache Cache { get; }

    public ModelTranslator(UserManager users, GroupManager groups, ClubManager clubs, MapManager maps, RequestCache cache, IOnlineStateManager? onlineStates = null)
    {
        this.onlineStates = onlineStates;
        this.users = users;
        this.groups = groups;
        this.clubs = clubs;
        this.maps = maps;
        Cache = cache;
    }

    public APIChatChannel ToAPI(ChatChannel channel)
    {
        var club = channel.Club is null ? null : clubs.Get(channel.Club.Value);

        return new APIChatChannel
        {
            Name = channel.Name,
            Type = channel.Type,
            UserCount = channel.Users.Count,
            Target1 = channel.Target1 is not null && users.TryGet(channel.Target1.Value, out var t1) ? ToAPI(t1) : null,
            Target2 = channel.Target2 is not null && users.TryGet(channel.Target2.Value, out var t2) ? ToAPI(t2) : null,
            Club = club is null ? null : ToAPI(club)
        };
    }

    public APIScore ToAPI(Score score, List<ScoreIncludes>? include = null)
    {
        var map = maps.GetMap(score.MapID);

        var apiScore = new APIScore
        {
            ID = score.ID,
            User = users.TryGet(score.UserID, out var u) ? ToAPI(u) : APIUser.CreateUnknown(score.UserID),
            Time = score.TimeLong,
            Mode = map?.Mode ?? 0,
            Mods = score.Mods,
            PerformanceRating = score.PerformanceRating,
            TotalScore = score.TotalScore,
            Accuracy = score.Accuracy,
            Rank = score.Grade,
            MaxCombo = score.MaxCombo,
            FlawlessCount = score.FlawlessCount,
            PerfectCount = score.PerfectCount,
            GreatCount = score.GreatCount,
            AlrightCount = score.AlrightCount,
            OkayCount = score.OkayCount,
            MissCount = score.MissCount,
            ScrollSpeed = score.ScrollSpeed
        };

        if (include == null || include.Count == 0)
            return apiScore;

        if (include.Contains(ScoreIncludes.Map))
            apiScore.Map = map is not null ? ToAPI(map) : APIMap.CreateUnknown(score.MapID);

        return apiScore;
    }

    public APIUser ToAPI(User user, long reqID = -1, int mode = 0, UserIncludes include = 0, UserExclude exclude = 0)
    {
        var u = new APIUser
        {
            ID = user.ID,
            SteamID = user.SteamID,
            Username = user.Username,
            DisplayName = user.DisplayName,
            AvatarHash = user.AvatarHash,
            BannerHash = user.BannerHash,
            HasAnimatedAvatar = user is { HasAnimatedAvatar: true, IsSupporter: true },
            HasAnimatedBanner = user is { HasAnimatedBanner: true, IsSupporter: true },
            AboutMe = user.AboutMe,
            Pronouns = user.Pronouns,
            CountryCode = user.CountryCode,
            Groups = user.GroupIDs.Select(x => groups.Get(x)).OfType<Group>().Select(x => ToAPI(x)).ToList(),
            IsOnline = onlineStates?.IsOnline(user.ID) ?? false,
            IsSupporter = user.IsSupporter
        };

        var paint = user.GetPaint(users);
        if (paint != null) u.NamePaint = ToAPI(paint);

        if (!exclude.HasFlagFast(UserExclude.Club))
        {
            var club = clubs.GetWhereUserIsMember(user.ID);
            u.Club = club != null ? ToAPI(club) : null;
        }

        if (u.IsOnline)
        {
            var act = onlineStates?.GetActivity(u.ID);
            if (act != null) u.Activity = act;
        }

        if (include.HasFlagFast(UserIncludes.CreatedAt))
            u.CreatedAt = user.CreatedAt;
        if (include.HasFlagFast(UserIncludes.LastLogin))
            u.LastLogin = user.LastLogin;
        if (include.HasFlagFast(UserIncludes.Email))
            u.Email = user.Email;
        if (include.HasFlagFast(UserIncludes.Flags))
            u.Flags = (long)user.BanFlags;

        if (include.HasFlagFast(UserIncludes.Following) && reqID >= 0)
            u.Following = users.GetFollowState(reqID, user.ID);

        if (include.HasFlagFast(UserIncludes.Socials))
        {
            u.Socials = new APIUserSocials
            {
                Twitter = user.Socials.Twitter,
                Twitch = user.Socials.Twitch,
                YouTube = user.Socials.YouTube,
                Discord = user.Socials.Discord
            };
        }

        if (include.HasFlagFast(UserIncludes.Statistics))
        {
            var stats = new APIUserStatistics
            {
                MaxCombo = user.MaxCombo,
                RankedScore = user.RankedScore,
                OverallAccuracy = user.OverallAccuracy,
                CountryRank = user.GetCountryRank(Cache, mode),
                GlobalRank = user.GetGlobalRank(Cache, mode)
            };

            if (mode != 0)
            {
                var m = user.GetModeStatistics(mode);
                stats.OverallRating = m.OverallRating;
                stats.PotentialRating = m.PotentialRating;
            }
            else
            {
                stats.OverallRating = user.OverallRating;
                stats.PotentialRating = user.PotentialRating;
            }

            u.Statistics = stats;
        }

        return u;
    }

    public APINamePaint ToAPI(NamePaint paint) => new()
    {
        ID = paint.ID,
        Name = paint.Name,
        Colors = paint.Colors.Select(c => new APIGradientColor
        {
            Color = c.Color,
            Position = c.Position
        }).ToList()
    };

    public APIMapSet ToAPI(MapSet set, MapSetInclude include = 0, long? userid = null, MapIncludes mapInclude = 0)
    {
        var creator = Cache.Users.Get(set.CreatorID);

        var api = new APIMapSet
        {
            ID = set.ID,
            Creator = creator is null ? APIUser.CreateUnknown(set.CreatorID) : ToAPI(creator),
            Maps = set.GetMaps(Cache).Select(map => ToAPI(map, mapInclude, set, userid)).ToList(),
            Title = set.Title,
            TitleRomanized = set.SortingTitle,
            Artist = set.Artist,
            ArtistRomanized = set.SortingArtist,
            Source = set.GetSource(Cache),
            Flags = set.Flags,
            Tags = set.GetTags(Cache),
            Status = (int)set.Status,
            DateSubmitted = set.Submitted.ToUnixTimeSeconds(),
            DateRanked = set.DateRanked?.ToUnixTimeSeconds(),
            LastUpdated = set.LastUpdated.ToUnixTimeSeconds(),
            UpVotes = set.UpVotes,
            DownVotes = set.DownVotes,
            ShowModActions = maps.HasActions(set.ID),
        };

        if (include.HasFlagFast(MapSetInclude.QueueInfo))
            api.QueueVotes = set.QueueVotes.Select(x => x.Approve).ToList();

        if (userid > 0)
            api.Favorite = maps.HasFavorite(userid.Value, set.ID);

        return api;
    }

    public APIMap ToAPI(Map map, MapIncludes include = 0, MapSet? set = null, long? userid = null)
    {
        var mappers = map.MapperIDs.Select(Cache.Users.Get).ToList();

        var m = new APIMap
        {
            ID = map.ID,
            MapSetID = map.SetID,
            Mappers = mappers.Select((x, i) => x != null ? ToAPI(x) : APIUser.CreateUnknown(map.MapperIDs[i])).ToList(),
            Difficulty = map.DifficultyName,
            SHA256Hash = map.SHA256Hash,
            Mode = map.Mode,
            Status = (int)(set?.Status ?? Cache.MapSets.Get(map.SetID)?.Status ?? MapStatus.Unsubmitted),
            Title = map.Title,
            TitleRomanized = map.SortingTitle,
            Artist = map.Artist,
            ArtistRomanized = map.SortingArtist,
            Source = map.Source,
            Tags = map.Tags,
            BPM = map.BPM,
            Length = map.Length,
            Rating = map.Rating,
            MaxCombo = map.MaxCombo,
            NoteCount = map.Hits,
            LongNoteCount = map.LongNotes,
            NotesPerSecond = map.NotesPerSecond,
            AccuracyDifficulty = map.AccuracyDifficulty,
            HealthDifficulty = map.HealthDifficulty,
            Effects = map.Effects
        };

        if (userid > 0)
            m.HasVotedRate = maps.HasRateVoted(userid.Value, map.ID);

        if (include.HasFlagFast(MapIncludes.FileName))
            m.FileName = map.FileName;

        if (include.HasFlagFast(MapIncludes.Claims))
            addClaimInfo(m);

        return m;

        void addClaimInfo(APIMap am)
        {
            var owned = clubs.GetClaim(map.ID);
            if (owned is null || owned.ClubID <= 0) return;

            var ownedScore = clubs.GetScore(owned.ClubID, owned.MapID);
            if (ownedScore is null) return;

            var ownedClub = Cache.Clubs.Get(owned.ClubID);

            am.ClaimOwned = new APIMapClaim
            {
                Club = ownedClub is not null ? ToAPI(ownedClub) : APIClub.CreateUnknown(owned.ClubID),
                Score = ToAPI(ownedScore)
            };

            if (userid is null or <= 0) return;

            var userClub = clubs.GetWhereUserIsMember(userid.Value);
            if (userClub is null) return;

            var clubScore = clubs.GetScore(userClub.ID, map.ID);
            if (clubScore is null) return;

            am.ClaimYourClub = new APIMapClaim
            {
                Club = ToAPI(userClub),
                Score = ToAPI(clubScore)
            };
        }
    }

    public APIChatMessage ToAPI(ChatMessage message)
    {
        var sender = Cache.Users.Get(message.SenderID);

        return new APIChatMessage
        {
            ID = message.ID.ToString(),
            CreatedAtUnix = message.CreatedAt.ToUnixTimeSeconds(),
            Content = message.Content,
            Channel = message.Channel,
            Sender = sender is not null ? ToAPI(sender) : APIUser.CreateUnknown(message.SenderID)
        };
    }

    public APIClub ToAPI(Club club, ClubIncludes include = 0)
    {
        var c = new APIClub
        {
            ID = club.ID,
            Name = club.Name,
            Tag = club.Tag,
            IconHash = club.IconHash,
            BannerHash = club.BannerHash,
            MemberCount = club.Members.Count,
            Colors = club.Colors.Select(c => new APIGradientColor
            {
                Color = c.Color,
                Position = c.Position
            }).ToList()
        };

        // don't bother with the rest if we don't need it
        if (include == 0)
            return c;

        if (include.HasFlagFast(ClubIncludes.Owner))
        {
            var owner = club.GetOwner(users, Cache);
            c.Owner = owner != null ? ToAPI(owner, exclude: UserExclude.Club) : APIUser.CreateUnknown(club.OwnerID);
        }

        if (include.HasFlagFast(ClubIncludes.JoinType))
            c.JoinType = club.JoinType;

        if (include.HasFlagFast(ClubIncludes.Members))
        {
            c.Members = club.GetMemberList(users ?? throw new InvalidOperationException($"{nameof(UserManager)} not provided."))
                            .Select(m => ToAPI(m, include: UserIncludes.LastLogin, exclude: UserExclude.Club))
                            .ToList();

            c.Members.RemoveAll(m => m.ID == -1);
        }

        if (include.HasFlagFast(ClubIncludes.Statistics))
        {
            var claims = clubs.GetAllClaimed(club.ID).Count();
            var pureMaps = maps.PureMapCount;

            c.Statistics = new APIClubStatistics
            {
                OverallRating = club.OverallRating,
                TotalScore = club.TotalScore,
                Rank = club.GetRank(Cache),
                TotalClaims = claims,
                ClaimPercentage = (claims / (double)pureMaps) * 100
            };
        }

        return c;
    }

    public APIClubScore ToAPI(ClubScore score)
    {
        var club = clubs.Get(score.ClubID);

        return new APIClubScore
        {
            Club = club is null ? APIClub.CreateUnknown(score.ClubID) : ToAPI(club),
            MapID = score.MapID,
            TotalScore = score.TotalScore,
            PerformanceRating = score.PerformanceRating,
            Accuracy = score.Accuracy
        };
    }

    public APIClubInvite ToAPI(ClubInvite invite)
    {
        var club = clubs.Get(invite.ClubID);
        var user = users.Get(invite.UserID);

        return new APIClubInvite
        {
            InviteCode = invite.InviteCode,
            Club = club != null ? ToAPI(club) : APIClub.CreateUnknown(invite.ClubID),
            User = user != null ? ToAPI(user) : APIUser.CreateUnknown(invite.UserID)
        };
    }

    public APIGroup ToAPI(Group group, bool members = false) => new()
    {
        ID = group.ID,
        Color = group.Color,
        Name = group.Name,
        Tag = group.Tag,
        Members = members ? users.InGroup(group.ID).Select(u => ToAPI(u)) : null
    };

    public APINotification? ToAPI(Notification notification)
    {
        var notif = new APINotification
        {
            Type = notification.Type,
            Time = notification.Time.ToUnixSeconds()
        };

        switch (notification.Type)
        {
            case NotificationType.ClubInvite:
            {
                if (notification.ClubInviteCode is null) return null;

                var invite = clubs.GetInvite(notification.ClubInviteCode);
                if (invite is null) return null;

                var club = Cache.Clubs.Get(invite.ClubID);

                notif.Data = JObject.FromObject(new ClubInviteNotification
                {
                    Club = club != null ? ToAPI(club) : APIClub.CreateUnknown(invite.ClubID),
                    InviteCode = invite.InviteCode
                });

                break;
            }
        }

        return notif;
    }
}
