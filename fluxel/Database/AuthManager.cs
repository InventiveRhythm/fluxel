using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using fluxel.Models.Auth;
using Midori.Database;
using Midori.Utils;
using MongoDB.Bson;

namespace fluxel.Database;

public class AuthManager
{
    private readonly IDatabaseTable<TimedCodeInfo> totpInfos;
    private readonly IDatabaseTable<AuthMethod> methods;

    public AuthManager(IDatabaseProvider db)
    {
        totpInfos = db.GetTable<TimedCodeInfo>("totp");
        methods = db.GetTable<AuthMethod>("auth");
    }

    #region Methods

    public void RegisterAuthMethod(long id, string method, BsonDocument data)
    {
        if (GetAuthMethod(id, method) != null)
            throw new InvalidOperationException($"User {id} already has auth method {method}!");

        var m = new AuthMethod { UserID = id, Method = method, Data = data };
        methods.Add(m);
    }

    public AuthMethod? GetAuthMethod(long id, string method)
    {
        var m = methods.Find(x => x.UserID == id && x.Method == method);
        return m.FirstOrDefault();
    }

    public AuthMethod? GetAuthMethod(Expression<Func<AuthMethod, bool>> match)
        => methods.Find(match).FirstOrDefault();

    #endregion

    #region Tokens

    private Dictionary<long, MultifactorToken> tokens { get; } = new();

    public string GenerateToken(long user)
    {
        var token = RandomizeUtils.GenerateRandomString(32, CharacterType.AllOfIt);
        var valid = DateTimeOffset.Now.AddMinutes(30).ToUnixTimeSeconds();

        tokens[user] = new MultifactorToken(token, valid);
        return token;
    }

    public bool IsValidToken(long user, string token)
    {
        if (!tokens.TryGetValue(user, out var tk))
            return false;

        if (tk.ValidUntil < DateTimeOffset.Now.ToUnixTimeSeconds())
            return false;

        return tk.Token == token;
    }

    #endregion

    #region TOTP

    public List<TimedCodeBackup> CreateTimeBased(long user, string key)
    {
        var totp = new TimedCodeInfo(user, key, Enumerable.Range(0, 12).Select(_ => TimedCodeBackup.Generate()).ToList());
        totpInfos.Add(totp);
        return totp.BackupCodes;
    }

    public bool HasCode(long user) => totpInfos.Find(x => x.UserID == user).FirstOrDefault() != null;

    public bool TryGetTimeBased(long user, [NotNullWhen(true)] out TimedCodeInfo? totp)
    {
        totp = totpInfos.Find(x => x.UserID == user).FirstOrDefault();
        return totp != null;
    }

    #endregion

    private class MultifactorToken
    {
        public string Token { get; set; }
        public long ValidUntil { get; set; }

        public MultifactorToken(string token, long validUntil)
        {
            Token = token;
            ValidUntil = validUntil;
        }
    }
}
