using System;
using System.Collections.Generic;
using System.Linq;
using fluxel.Models.OAuth;
using Midori.Database;
using Midori.Utils;

namespace fluxel.Database;

public class OAuthManager
{
    private readonly IDatabaseTable<OAuthApplication> applications;
    private readonly IDatabaseTable<OAuthToken> tokens;

    public IEnumerable<OAuthToken> AllExpired => tokens.Find(x => x.ExpireTime < DateTimeOffset.UtcNow.ToUnixTimeSeconds()).ToList();

    public OAuthManager(IDatabaseProvider db)
    {
        applications = db.GetTable<OAuthApplication>("oauth-apps");
        tokens = db.GetTable<OAuthToken>("oauth");
    }

    #region Applications

    public OAuthApplication? FindApplication(string id)
        => applications.Find(x => x.ClientID == id).FirstOrDefault();

    #endregion

    #region Tokens

    public OAuthToken? GetToken(string accessToken)
    {
        var token = tokens.Find(x => x.AccessToken == accessToken).FirstOrDefault();

        if (token == null)
            return null;

        if (token.ExpireTime < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        {
            tokens.Delete(x => x.AccessToken == accessToken);
            return null;
        }

        return token;
    }

    public void DeleteToken(OAuthToken token) => tokens.Delete(x => x.AccessToken == token.AccessToken);

    /// <summary>
    /// Create a new OAuth token for the specified user.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="expireHours">The time in hours until the token expires.</param>
    /// <param name="scopes">The scopes to assign to the token.</param>
    /// <returns>The created token.</returns>
    public OAuthToken CreateToken(long userId, long expireHours, params OAuthScopes[] scopes)
    {
        var token = new OAuthToken
        {
            UserID = userId,
            AccessToken = $"flx:{RandomizeUtils.GenerateRandomString(28, CharacterType.AllOfIt)}",
            Scopes = scopes,
            ExpireTime = DateTimeOffset.UtcNow.AddHours(expireHours).ToUnixTimeSeconds()
        };

        tokens.Add(token);
        return token;
    }

    #endregion
}
