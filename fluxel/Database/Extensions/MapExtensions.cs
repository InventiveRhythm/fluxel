using System.Collections.Generic;
using System.Linq;
using fluxel.Database.Helpers;
using fluxel.Models.Maps;
using fluXis.Online.API.Models.Clubs;
using fluXis.Online.API.Models.Maps;
using fluXis.Online.API.Models.Users;
using osu.Framework.Extensions.EnumExtensions;

namespace fluxel.Database.Extensions;

public static class MapExtensions
{
    public static APIMapSet ToAPI(this MapSet set, MapSetInclude include = 0, long? userid = null, MapIncludes mapInclude = 0)
    {
        var api = new APIMapSet
        {
            ID = set.ID,
            Creator = (set.Cache.Users.Get(set.CreatorID) ?? UserHelper.Get(set.CreatorID))?.ToAPI() ?? APIUser.CreateUnknown(set.CreatorID),
            Maps = set.MapsList.Select(map => map.ToAPI(mapInclude, set, userid)).ToList(),
            Title = set.Title,
            TitleRomanized = set.SortingTitle,
            Artist = set.Artist,
            ArtistRomanized = set.SortingArtist,
            Source = set.Source,
            Flags = set.Flags,
            Tags = set.Tags,
            Status = (int)set.Status,
            DateSubmitted = set.Submitted.ToUnixTimeSeconds(),
            DateRanked = set.DateRanked?.ToUnixTimeSeconds(),
            LastUpdated = set.LastUpdated.ToUnixTimeSeconds(),
            UpVotes = set.UpVotes,
            DownVotes = set.DownVotes,
            ShowModActions = MapSetHelper.HasActions(set.ID),
        };

        if (include.HasFlagFast(MapSetInclude.QueueInfo))
            api.QueueVotes = set.QueueVotes.Select(x => x.Approve).ToList();

        if (userid > 0)
            api.Favorite = MapSetHelper.HasFavorite(userid.Value, set.ID);

        return api;
    }

    public static APIMap ToAPI(this Map map, MapIncludes include = 0, MapSet? set = null, long? userid = null)
    {
        var m = new APIMap
        {
            ID = map.ID,
            MapSetID = map.SetID,
            Mapper = map.Cache.Users.Get(map.MapperID)?.ToAPI() ?? APIUser.CreateUnknown(map.MapperID),
            Difficulty = map.DifficultyName,
            SHA256Hash = map.SHA256Hash,
            Mode = map.Mode,
            Status = (int)(set?.Status ?? map.Cache.MapSets.Get(map.SetID)?.Status ?? MapStatus.Unsubmitted),
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
            LandminesCount = map.Landmines,
            NotesPerSecond = map.NotesPerSecond,
            AccuracyDifficulty = map.AccuracyDifficulty,
            HealthDifficulty = map.HealthDifficulty,
            Effects = map.Effects
        };

        if (userid > 0)
            m.HasVotedRate = MapHelper.HasVoted(userid.Value, map.ID);

        if (include.HasFlagFast(MapIncludes.FileName))
            m.FileName = map.FileName;

        if (include.HasFlagFast(MapIncludes.Claims))
            addClaimInfo(m, userid);

        return m;
    }

    private static void addClaimInfo(APIMap map, long? userid = null)
    {
        var owned = ClubHelper.GetClaim(map.ID);
        if (owned is null || owned.ClubID <= 0) return;

        var ownedScore = ClubHelper.GetScore(owned.ClubID, owned.MapID);
        if (ownedScore is null) return;

        map.ClaimOwned = new APIMapClaim
        {
            Club = ClubHelper.Get(owned.ClubID)?.ToAPI() ?? APIClub.CreateUnknown(owned.ClubID),
            Score = ownedScore.ToAPI()
        };

        if (userid is null or <= 0) return;

        var club = ClubHelper.GetWhereUserIsMember(userid.Value);
        if (club is null) return;

        var clubScore = ClubHelper.GetScore(club.ID, map.ID);
        if (clubScore is null) return;

        map.ClaimYourClub = new APIMapClaim
        {
            Club = club.ToAPI(),
            Score = clubScore.ToAPI()
        };
    }

    public static bool AllowScores(this Map map)
    {
        var set = map.Cache.MapSets.Get(map.SetID);
        if (set is null) return false;

        return set.Status >= MapStatus.Pure;
    }

    private static int getVote(this MapSet map, long user) => map.Votes.GetValueOrDefault(user.ToString(), 0);

    public static void SetVote(this MapSet map, long user, int vote) => map.Votes[user.ToString()] = vote;

    public static APIMapVotes GetVotes(this MapSet map, long user = 0) => new()
    {
        MapID = map.ID,
        YourVote = user == 0 ? 0 : map.getVote(user),
        UpVotes = map.UpVotes,
        DownVotes = map.DownVotes
    };
}
