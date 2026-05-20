using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using fluxel.Authentication;
using fluxel.Models;
using fluxel.Models.Relations;
using fluxel.Models.Users;
using fluxel.Models.Users.Equipment;
using fluxel.Utils;
using fluXis.Online.API.Models.Users;
using Midori.Database;
using Midori.Utils;

namespace fluxel.Database;

public class UserManager
{
    public const string TABLE_NAME = "users";

    private readonly IDatabaseTable<FollowRelation> follows;
    private readonly IDatabaseTable<UserLogin> logins;
    private readonly IDatabaseTable<NamePaint> paints;
    private readonly IDatabaseTable<UserSession> sessions;
    private readonly IDatabaseTable<User> users;

    private readonly CounterManager counters;

    public List<UserLogin> AllLogins => logins.Find(x => true).ToList();
    public List<User> AllUsers => users.Find(x => true).ToList();

    public long UserCount => users.Count(_ => true);

    public UserManager(IDatabaseProvider db, CounterManager counters)
    {
        follows = db.GetTable<FollowRelation>("follows");
        logins = db.GetTable<UserLogin>("user-logins");
        paints = db.GetTable<NamePaint>("paints");
        sessions = db.GetTable<UserSession>("sessions");
        users = db.GetTable<User>(TABLE_NAME);
        this.counters = counters;
    }

    public User Add(string username, string email, string password, string? country)
    {
        var user = new User
        {
            ID = counters.GetNext(CounterType.User),
            Username = username,
            Email = email,
            Password = PasswordAuth.HashPassword(password),
            CountryCode = country
        };

        users.Add(user);
        return user;
    }

    public List<User> InGroup(string group) => users.Find(u => u.GroupIDs.Contains(group)).ToList();
    public bool UsernameExists(string username) => users.Find(u => string.Equals(u.Username, username, StringComparison.CurrentCultureIgnoreCase)).Any();

    #region Query

    public User? Get(long id) => users.Find(u => u.ID == id).FirstOrDefault();
    public User? Get(string name) => users.Find(u => string.Equals(u.Username, name, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();

    public bool TryGet(long id, [NotNullWhen(true)] out User? user)
    {
        user = Get(id);
        return user != null;
    }

    public bool TryGet(string name, [NotNullWhen(true)] out User? user)
    {
        user = Get(name);
        return user != null;
    }

    public IEnumerable<User> GetMany(IEnumerable<long> ids) => users.Find(u => ids.Contains(u.ID)).ToList();
    public User? GetByDiscordID(ulong id) => users.Find(x => x.DiscordID == id).FirstOrDefault();

    #endregion

    #region Query (E-Mail)

    public User? GetByEmail(string email)
        => users.Find(u => string.Equals(u.Email, email, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();

    public bool TryGetByEmail(string email, [NotNullWhen(true)] out User? user)
    {
        user = GetByEmail(email);
        return user != null;
    }

    public User? GetByKoFiEmail(string email)
        => users.Find(u => string.Equals(u.KoFiEmail, email, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();

    #endregion

    #region Updating

    private ConcurrentDictionary<long, object> userLocks { get; } = new();

    public User UpdateLocked(long id, Action<User>? action)
    {
        var lk = userLocks.GetOrAdd(id, _ => new object());

        lock (lk)
        {
            var user = Get(id);

            if (user is null)
                throw new ArgumentNullException(nameof(id), "No user with the provided ID found.");

            action?.Invoke(user);
            users.Replace(x => x.ID == user.ID, user);
            return user;
        }
    }

    #endregion

    #region Online Logs

    private readonly object onlineLogLock = new();

    public void LogOnline(long id, bool online)
    {
        lock (onlineLogLock)
        {
            if (LastOnlineLogs().Contains(id) == online)
                return;

            logins.Add(new UserLogin
            {
                Time = DateTimeOffset.Now.ToUnixTimeSeconds(),
                UserID = id,
                IsOnline = online
            });
        }
    }

    public void ClearLogin(UserLogin login) => logins.Delete(x => x.ID == login.ID);

    public List<long> LastOnlineLogs()
    {
        var l = logins.Find(_ => true).ToList();
        var u = l.GroupBy(x => x.UserID);
        var online = u.Where(x => x.LastOrDefault()?.IsOnline == true);
        return online.Select(x => x.Key).ToList();
    }

    #endregion

    #region Sessions

    private UserSession? updateSession(UserSession? session)
    {
        if (session is null)
            return null;

        session.LastActivity = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        sessions.Replace(s => s.Token == session.Token, session);
        return session;
    }

    public UserSession? GetSessionFromToken(string token, bool update = true)
    {
        var session = sessions.Find(x => x.Token == token).FirstOrDefault();

        if (update)
            session = updateSession(session);

        return session;
    }

    public List<UserSession> GetSessionsFromUser(long id)
        => sessions.Find(x => x.UserID == id).ToList();

    public UserSession? GetSessionFromIP(long id, string ip, bool update = true)
    {
        var session = sessions.Find(x => x.UserID == id && x.IP == ip && !x.UserAgent.StartsWith("game")).FirstOrDefault();

        if (update)
            session = updateSession(session);

        return session;
    }

    public async Task<UserSession> CreateSession(long user, string ip, string agent)
    {
        var country = await IpUtils.GetCountryCode(ip);
        var token = generateToken();

        while (GetSessionFromToken(token, false) != null)
            token = generateToken();

        var session = new UserSession
        {
            Token = token,
            UserID = user,
            IP = ip,
            Country = country ?? "xx",
            UserAgent = agent,
            LastActivity = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        sessions.Add(session);
        return session;
    }

    public void RemoveSessions(Expression<Func<UserSession, bool>> match) => sessions.DeleteMultiple(match);

    private static string generateToken() => RandomizeUtils.GenerateRandomString(32, CharacterType.AllOfIt);

    #endregion

    #region Equipment

    public void AddPaint(NamePaint paint) => paints.Add(paint);
    public NamePaint? GetPaint(string id) => paints.Find(p => p.ID == id).FirstOrDefault();

    public void AddPaintIfMissing(NamePaint paint)
    {
        if (GetPaint(paint.ID) is not null)
            return;

        AddPaint(paint);
    }

    #endregion

    #region Follows

    public void StartFollow(long follower, long followee)
    {
        if (IsFollowing(follower, followee))
            return;

        // pov: self love
        if (follower == followee)
            return;

        var relation = new FollowRelation
        {
            FollowerID = follower,
            FolloweeID = followee
        };

        follows.Add(relation);
    }

    public void StopFollow(long follower, long followee) => follows.Delete(x => x.FollowerID == follower && x.FolloweeID == followee);

    public bool IsFollowing(long follower, long followee) => follows.Find(x => x.FollowerID == follower && x.FolloweeID == followee).Any();
    public bool Mutual(long user1, long user2) => IsFollowing(user1, user2) && IsFollowing(user2, user1);

    public UserFollowState GetFollowState(long follower, long followee)
    {
        var a = IsFollowing(follower, followee);
        var b = IsFollowing(followee, follower);

        return a switch
        {
            true when b => UserFollowState.Mutual,
            true => UserFollowState.Following,
            false when b => UserFollowState.Followed,
            _ => UserFollowState.None
        };
    }

    public List<long> GetFollowers(long followee) => follows.Find(x => x.FolloweeID == followee).ToList().Select(x => x.FollowerID).ToList();
    public List<long> GetFollowing(long follower) => follows.Find(x => x.FollowerID == follower).ToList().Select(x => x.FolloweeID).ToList();

    #endregion
}
