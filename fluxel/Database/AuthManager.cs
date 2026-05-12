using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using fluxel.Models.Auth;
using Midori.Database;
using Midori.Utils;

namespace fluxel.Database;

public class AuthManager
{
    private readonly IDatabaseTable<TimedCodeInfo> totpInfos;

    public AuthManager(IDatabaseProvider db)
    {
        totpInfos = db.GetTable<TimedCodeInfo>("totp");
    }

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
