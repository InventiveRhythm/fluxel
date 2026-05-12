using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using fluxel.Models;
using fluxel.Models.Clubs;
using fluxel.Models.Scores;
using fluxel.Models.Users;
using Midori.Database;
using Midori.Utils;

namespace fluxel.Database;

public class ClubManager
{
    public const string TABLE_NAME = "clubs";

    private readonly IDatabaseTable<Club> clubs;
    private readonly IDatabaseTable<ClubScore> scores;
    private readonly IDatabaseTable<ClubClaim> claims;
    private readonly IDatabaseTable<ClubInvite> invites;
    private readonly CounterManager counters;

    public ClubManager(IDatabaseProvider db, CounterManager counters)
    {
        clubs = db.GetTable<Club>(TABLE_NAME);
        scores = db.GetTable<ClubScore>("club-scores");
        claims = db.GetTable<ClubClaim>("club-claims");
        invites = db.GetTable<ClubInvite>("club-invites");
        this.counters = counters;
    }

    #region Clubs themselves

    public List<Club> All => clubs.Find(m => true).ToList();

    public Club? Get(long id) => clubs.Find(m => m.ID == id).FirstOrDefault();

    public Club? ByTag(string tag) => clubs.Find(m => m.Tag == tag).FirstOrDefault();
    public Club? ByName(string name) => clubs.Find(m => m.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();

    public void Add(Club club)
    {
        club.ID = counters.GetNext(CounterType.Club);
        clubs.Add(club);
    }

    public void Update(Club club) => clubs.Replace(m => m.ID == club.ID, club);

    public Club? GetWhereUserIsMember(User user) => GetWhereUserIsMember(user.ID);
    public Club? GetWhereUserIsMember(long userId) => clubs.Find(m => m.Members.Contains(userId)).FirstOrDefault();

    public bool TryGetWhereUserIsMember(User user, [NotNullWhen(true)] out Club? club)
        => TryGetWhereUserIsMember(user.ID, out club);

    public bool TryGetWhereUserIsMember(long uid, [NotNullWhen(true)] out Club? club)
    {
        club = GetWhereUserIsMember(uid);
        return club != null;
    }

    #endregion

    #region Scores

    public List<ClubScore> GetScores(long clubId) => scores.Find(s => s.ClubID == clubId).ToList();

    public List<ClubScore> GetScoresOnMap(long mapId) => scores.Find(s => s.MapID == mapId).ToList();

    public ClubScore? GetScore(long clubId, long mapId, bool createIfNull = false)
    {
        var first = scores.Find(s => s.ClubID == clubId && s.MapID == mapId).FirstOrDefault();

        if (first == null && createIfNull)
        {
            first = new ClubScore
            {
                ClubID = clubId,
                MapID = mapId
            };

            scores.Add(first);
        }

        return first;
    }

    public void UpdateScore(ClubScore score)
    {
        if (score.PerformanceRating <= 0)
        {
            scores.Delete(s => s.ID == score.ID);
            return;
        }

        scores.Replace(s => s.ID == score.ID, score);
    }

    #endregion

    #region Claims

    public ClubClaim? GetClaim(long mapId, bool createIfNull = false)
    {
        var first = claims.Find(s => s.MapID == mapId).FirstOrDefault();

        if (first == null && createIfNull)
        {
            first = new ClubClaim { MapID = mapId };
            claims.Add(first);
        }

        return first;
    }

    public void UpdateClaim(ClubClaim claim)
    {
        if (claim.ClubID <= 0)
        {
            claims.Delete(s => s.MapID == claim.MapID);
            return;
        }

        claims.Replace(s => s.MapID == claim.MapID, claim);
    }

    public IEnumerable<ClubClaim> GetAllClaimed(long club)
        => claims.Find(c => c.ClubID == club).ToList();

    #endregion

    #region Invites

    private List<ClubInvite> allInvites => invites.Find(x => true).ToList();

    public ClubInvite CreateInvite(long club, long user)
    {
        var invite = new ClubInvite
        {
            InviteCode = createCode(),
            ClubID = club,
            UserID = user
        };

        invites.Add(invite);
        return invite;
    }

    public ClubInvite? GetInvite(string code) => invites.Find(x => x.InviteCode == code).FirstOrDefault();

    public void RemoveForUser(long user) => invites.DeleteMultiple(x => x.UserID == user);

    private string createCode()
    {
        var existingCodes = allInvites.Select(x => x.InviteCode).ToList();
        var code = "";

        while (existingCodes.Contains(code) || string.IsNullOrEmpty(code))
            code = RandomizeUtils.GenerateRandomString(7);

        return code;
    }

    #endregion
}
