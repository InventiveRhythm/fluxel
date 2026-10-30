using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using fluxel.Components.IPC;
using fluxel.Models;
using fluxel.Models.Chat;
using fluxel.Models.Clubs;
using fluxel.Models.Cosmetics;
using fluxel.Models.Groups;
using fluxel.Models.Maps;
using fluxel.Models.Maps.Flags;
using fluxel.Models.Notifications;
using fluxel.Models.Scores;
using fluxel.Models.Users;
using fluxel.Models.Users.Flags;
using fluxel.Utils;
using fluXis.Online.API.Models.Chat;
using fluXis.Online.API.Models.Clubs;
using fluXis.Online.API.Models.Groups;
using fluXis.Online.API.Models.Maps;
using fluXis.Online.API.Models.Maps.Modding;
using fluXis.Online.API.Models.Notifications;
using fluXis.Online.API.Models.Notifications.Data;
using fluXis.Online.API.Models.Other;
using fluXis.Online.API.Models.Scores;
using fluXis.Online.API.Models.Users;
using fluXis.Online.API.Models.Users.Equipment;
using Microsoft.EntityFrameworkCore;
using Midori.Utils;
using Newtonsoft.Json.Linq;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Extensions.EnumExtensions;
using rhym;
using ClubInviteNotification = fluxel.Models.Notifications.ClubInviteNotification;

namespace fluxel.Components;

/// <summary>
/// Helper class that converts all database models to their respective API variants.
/// </summary>
public class ModelTranslator
{
    public readonly DatabaseContext Database;
    private readonly Valkey? valkey;

    public ModelTranslator(DatabaseContext database, Valkey? valkey = null)
    {
        this.Database = database;
        this.valkey = valkey;
    }

    public APIChatChannel ToAPI(ChatChannel channel)
    {
        Database.Entry(channel).Reference(x => x.TargetUser1).Load();
        Database.Entry(channel).Reference(x => x.TargetUser2).Load();

        var club = channel.ClubID is null ? null : Database.Clubs.Find(channel.ClubID.Value);

        return new APIChatChannel
        {
            Name = channel.ID,
            Type = channel.Type,
            UserCount = Database.Entry(channel).Collection(c => c.Members).Query().Count(),
            Target1 = ToAPI(channel.TargetUser1),
            Target2 = ToAPI(channel.TargetUser2),
            Club = ToAPI(club)
        };
    }

    public APIScore ToAPI(Score score, List<ScoreIncludes>? include = null)
    {
        Database.Entry(score).Reference(x => x.Map).Load();
        Database.Entry(score).Reference(x => x.User).Load();

        var s = new APIScore
        {
            ID = score.ID,
            User = ToAPI(score.User) ?? APIUser.CreateUnknown(score.UserID),
            Time = score.Time.ToUnixSeconds(),
            Mode = score.Map?.Mode ?? "unknown:unknown",
            Mods = string.Join(',', score.Mods),
            PerformanceRating = score.PerformanceRating,
            TotalScore = score.NumericScore,
            Accuracy = score.Accuracy,
            Rank = score.Rank,
            MaxCombo = score.HighestCombo,
            FlawlessCount = score.FlawlessCount,
            PerfectCount = score.PerfectCount,
            GreatCount = score.GreatCount,
            AlrightCount = score.AlrightCount,
            OkayCount = score.OkayCount,
            MissCount = score.MissCount,
            ScrollSpeed = score.ScrollSpeed
        };

        if (include == null || include.Count == 0)
            return s;

        if (include.Contains(ScoreIncludes.Map))
            s.Map = ToAPI(score.Map) ?? APIMap.CreateUnknown(score.MapID);

        return s;
    }

    [return: NotNullIfNotNull(nameof(user))]
    public APIUser? ToAPI(User? user, long reqID = -1, ResourceLocation? mode = null, UserIncludes include = 0, UserExcludes exclude = 0)
    {
        if (user is null)
            return null;

        Database.Entry(user).Reference(x => x.Profile).Load();

        var u = new APIUser
        {
            ID = user.ID,
            SteamID = user.SteamID,
            Username = user.Username,
            DisplayName = user.Profile?.DisplayName,
            AvatarHash = user.Profile?.AvatarHash,
            BannerHash = user.Profile?.BannerHash,
            HasAnimatedAvatar = user.IsSupporter && (user.Profile?.HasAnimatedAvatar ?? false),
            HasAnimatedBanner = user.IsSupporter && (user.Profile?.HasAnimatedBanner ?? false),
            AboutMe = user.Profile?.AboutMe,
            Pronouns = user.Profile?.Pronouns,
            CountryCode = user.CountryCode,
            Groups =
            [
                .. Database.Entry(user).Collection(x => x.GroupMemberships)
                           .Query().Include(x => x.Group)
                           .ToArray().Select(x => ToAPI(x.Group!))
            ],
            IsOnline = valkey?.Database.SetContains(Valkey.UsersOnline, user.ID) ?? false,
            IsSupporter = user.IsSupporter
        };

        // TODO:
        // var paint = user.GetPaint(users);
        // if (paint != null) u.NamePaint = ToAPI(paint);

        if (!exclude.HasFlagFast(UserExcludes.Club))
        {
            Database.Entry(user).Reference(x => x.Club).Load();
            u.Club = ToAPI(user.Club);
        }

        if (u.IsOnline)
        {
            var val = valkey?.Database.StringGet($"{Valkey.USER_ACTIVITY}:{user.ID}");
            if (!(val?.IsNull ?? true)) u.Activity = ((string)val!).Deserialize<APIActivity>();
        }

        if (include.HasFlagFast(UserIncludes.CreatedAt))
            u.CreatedAt = user.CreatedAt.ToUnixSeconds();
        if (include.HasFlagFast(UserIncludes.LastLogin))
            u.LastLogin = user.LastLogin.ToUnixSeconds();
        if (include.HasFlagFast(UserIncludes.Flags))
            u.Flags = (long)user.BanFlags;

        // TODO
        // if (include.HasFlagFast(UserIncludes.Following) && reqID >= 0)
        // u.Following = users.GetFollowState(reqID, user.ID);

        if (include.HasFlagFast(UserIncludes.Statistics))
        {
            // TODO
            /*var stats = new APIUserStatistics
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

            u.Statistics = stats;*/
        }

        return u;
    }

    public APINamePaint ToAPI(NamePaint paint) => new()
    {
        ID = paint.ID,
        Name = paint.Name
    };

    [return: NotNullIfNotNull(nameof(set))]
    public APIMapSet? ToAPI(MapSet? set, MapSetIncludes include = 0, long? userid = null, MapIncludes mapInclude = 0)
    {
        if (set is null)
            return null;

        Database.Entry(set).Collection(x => x.Maps).Load();
        Database.Entry(set).Reference(x => x.User).Load();

        var api = new APIMapSet
        {
            ID = set.ID,
            Creator = ToAPI(set.User) ?? APIUser.CreateUnknown(set.UserID),
            Maps = [.. set.Maps.Select(m => ToAPI(m, mapInclude, set, userid))],
            Title = set.Title,
            TitleRomanized = set.TitleRomanized ?? set.Title,
            Artist = set.Artist,
            ArtistRomanized = set.ArtistRomanized ?? set.Artist,
            Source = set.Maps.Select(x => x.Sources.GetValueOrDefault("audio", string.Empty))
                        .GroupBy(x => x).OrderByDescending(x => x.Count())
                        .FirstOrDefault()?.Key ?? string.Empty,
            Flags = set.Flags,
            Tags = [.. set.Maps.SelectMany(x => x.Tags).Distinct()],
            Status = (int)set.Status,
            DateSubmitted = set.Submitted.ToUnixSeconds(),
            DateRanked = set.DateRanked?.ToUnixSeconds(),
            LastUpdated = set.LastUpdated.ToUnixSeconds(),
            UpVotes = Database.Entry(set).Collection(x => x.UserStates).Query().Count(x => x.Vote == true),
            DownVotes = Database.Entry(set).Collection(x => x.UserStates).Query().Count(x => x.Vote == false),
            ShowModActions = Database.ModdingActions.Any(x => x.MapSetID == set.ID)
        };

        if (include.HasFlagFast(MapSetIncludes.QueueInfo) && set.QueueSubmitted != null)
        {
            var actions = Database.ModdingActions.Where(x => x.MapSetID == set.ID);

            api.QueueInfo = new APIQueueInfo
            {
                // why does this return an array of booleans
                // what was I even thinking
                Votes = [.. actions.Where(x => x.Created >= set.QueueSubmitted && x.Type == APIModdingActionType.Approve).Select(_ => true)],
                LastAction = actions.OrderBy(x => x.Created).Last().ToAPI(Database, x => ToAPI(x))
            };
        }

        if (userid > 0)
            api.Favorite = Database.MapSetUserStates.Find(set.ID, userid)?.Favorite ?? false;

        return api;
    }

    [return: NotNullIfNotNull(nameof(map))]
    public APIMap? ToAPI(Map? map, MapIncludes include = 0, MapSet? set = null, long? userid = null)
    {
        if (map is null)
            return null;

        var m = new APIMap
        {
            ID = map.ID,
            MapSetID = map.MapSetID,
            Mappers = [.. map.MapperIDs.Select(x => Database.Users.Find(x)).Zip(map.MapperIDs).Select(x => ToAPI(x.First) ?? APIUser.CreateUnknown(x.Second))],
            Difficulty = map.DifficultyName,
            SHA256Hash = map.SHA256Hash,
            Mode = map.Mode,
            Status = (int)(Database.MapSets.Find(map.MapSetID)?.Status ?? MapSetStatus.Unsubmitted),
            Title = map.Title,
            TitleRomanized = map.TitleRomanized ?? map.Title,
            Artist = map.Artist,
            ArtistRomanized = map.ArtistRomanized ?? map.Artist,
            Source = map.Sources.GetValueOrDefault("audio", string.Empty),
            Tags = string.Join(',', map.Tags),
            BPM = map.BPM,
            Length = map.Length,
            Rating = map.DifficultyRating,
            // MaxCombo = map.MaxCombo, // TODO
            // NoteCount = map.Hits, // TODO
            // LongNoteCount = map.LongNotes, // TODO
            // NotesPerSecond = map.NotesPerSecond, // TODO
            AccuracyDifficulty = map.AccuracyDifficulty,
            HealthDifficulty = map.HealthDifficulty,
            // Effects = map.Effects, // TODO
            Color = map.AccentColor.ToRGBA()
        };

        if (userid > 0)
            m.HasVotedRate = Database.MapUserStates.Find(map.ID, userid.Value)?.RateChart > 0;

        if (include.HasFlagFast(MapIncludes.FileName))
            m.FileName = map.FileName;

        if (include.HasFlagFast(MapIncludes.Claims))
            addClaimInfo(m);

        return m;

        void addClaimInfo(APIMap am)
        {
            var owned = Database.ClubClaims.Find(am.ID);
            if (owned is null || owned.ClubID <= 0) return;

            // TODO
            /*var ownedScore = clubs.GetScore(owned.ClubID, owned.MapID);
            if (ownedScore is null) return;

            var ownedClub = Cache.Clubs.Get(owned.ClubID);

            am.ClaimOwned = new APIMapClaim
            {
                Club = ownedClub is not null ? ToAPI(ownedClub) : APIClub.CreateUnknown(owned.ClubID),
                Score = ToAPI(ownedScore)
            };

            if (userid is null or <= 0 || !users.TryGet(userid.Value, out var user)) return;

            var userClub = user.GetClub(clubs);
            if (userClub is null) return;

            var clubScore = clubs.GetScore(userClub.ID, map.ID);
            if (clubScore is null) return;

            am.ClaimYourClub = new APIMapClaim
            {
                Club = ToAPI(userClub),
                Score = ToAPI(clubScore)
            };*/
        }
    }

    public APIChatMessage ToAPI(ChatMessage message)
    {
        Database.Entry(message).Reference(x => x.Author).Load();

        return new APIChatMessage
        {
            ID = message.ID,
            CreatedAtUnix = message.CreatedAt.ToUnixSeconds(),
            Content = message.Content,
            Channel = message.ChannelID,
            Sender = ToAPI(message.Author) ?? APIUser.CreateUnknown(message.AuthorID)
        };
    }

    [return: NotNullIfNotNull(nameof(club))]
    public APIClub? ToAPI(Club? club, ClubIncludes include = 0)
    {
        if (club is null)
            return null;

        var c = new APIClub
        {
            ID = club.ID,
            Name = club.Name,
            Tag = club.Tag,
            IconHash = club.IconHash,
            BannerHash = club.BannerHash,
            MemberCount = Database.Entry(club).Collection(x => x.Members).Query().Count(),
            Colors =
            [
                new APIGradientColor { Color = club.ColorStart.ToHex(), Position = 0 },
                new APIGradientColor { Color = club.ColorEnd.ToHex(), Position = 1 }
            ]
        };

        // don't bother with the rest if we don't need it
        if (include == 0)
            return c;

        if (include.HasFlagFast(ClubIncludes.Owner))
        {
            Database.Entry(club).Reference(x => x.Owner).Load();
            c.Owner = ToAPI(club.Owner) ?? APIUser.CreateUnknown(club.OwnerID);
        }

        if (include.HasFlagFast(ClubIncludes.JoinType))
            c.JoinType = club.JoinType;

        if (include.HasFlagFast(ClubIncludes.Members))
        {
            Database.Entry(club).Collection(x => x.Members).Load();
            c.Members = [.. club.Members.Select(m => ToAPI(m, include: UserIncludes.LastLogin, exclude: UserExcludes.Club))];
        }

        if (include.HasFlagFast(ClubIncludes.Statistics))
        {
            // TODO:
            /*var claims = clubs.GetAllClaimed(club.ID).Count();
            var pureMaps = maps.PureMapCount;

            c.Statistics = new APIClubStatistics
            {
                OverallRating = club.OverallRating,
                TotalScore = club.TotalScore,
                Rank = club.GetRank(Cache),
                TotalClaims = claims,
                ClaimPercentage = (claims / (double)pureMaps) * 100
            };*/
        }

        return c;
    }

    public APIClubScore ToAPI(ClubScore score)
    {
        Database.Entry(score).Reference(x => x.Club).Load();

        return new APIClubScore
        {
            Club = ToAPI(score.Club),
            MapID = score.MapID,
            TotalScore = score.NumericScore,
            PerformanceRating = score.PerformanceRating,
            Accuracy = score.Accuracy
        };
    }

    public APIClubInvite ToAPI(ClubInvite invite)
    {
        Database.Entry(invite).Reference(x => x.Club).Load();
        Database.Entry(invite).Reference(x => x.User).Load();

        return new APIClubInvite
        {
            InviteCode = invite.InviteCode,
            Club = ToAPI(invite.Club) ?? APIClub.CreateUnknown(invite.ClubID),
            User = ToAPI(invite.User) ?? APIUser.CreateUnknown(invite.UserID)
        };
    }

    public APIGroup ToAPI(Group group) => new()
    {
        ID = group.ID,
        Color = group.Color.ToHex(),
        Name = group.Name,
        Tag = group.Tag
    };

    public APINotification? ToAPI(Notification notification)
    {
        var notif = new APINotification
        {
            Time = notification.Time.ToUnixSeconds()
        };

        switch (notification)
        {
            case ClubInviteNotification ci:
            {
                var invite = Database.ClubInvites.Include(x => x.Club)
                                     .FirstOrDefault(x => x.InviteCode == ci.ClubInviteCode);

                if (invite is null)
                    return null;

                notif.Data = JObject.FromObject(new fluXis.Online.API.Models.Notifications.Data.ClubInviteNotification()
                {
                    Club = ToAPI(invite.Club) ?? APIClub.CreateUnknown(invite.ClubID),
                    InviteCode = invite.InviteCode
                });

                break;
            }

            case QueueNotification qu:
            {
                notif.Data = JObject.FromObject(new QueueUpdateNotification
                {
                    MapSet = ToAPI(Database.MapSets.Find(qu.MapSet)) ?? APIMapSet.CreateUnknown(qu.MapSet),
                    Type = qu.QueueAction
                });

                break;
            }
        }

        return notif;
    }
}
