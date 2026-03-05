using System;
using System.Net;
using fluxel.Constants;
using fluxel.Database.Helpers;
using fluxel.Models.Users;
using fluxel.Utils;
using Midori.Logging;
using Midori.Networking.WebSockets.Typed;

namespace fluxel.WebSocket;

public abstract class AuthenticatedSocket<S, C> : TypedWebSocketSession<S, C>
    where S : class where C : class
{
    public virtual Version SupportedVersion => new(2026, 124);

    public IPEndPoint? Address { get; private set; }
    public long UserID { get; set; }

    public User CurrentUser => UserHelper.Get(UserID) ?? throw new InvalidOperationException("Tried to get user before login.");

    protected override bool Authenticate(out string message)
    {
        message = "";

        var forwardedFor = Headers["X-Forwarded-For"];

        if (forwardedFor != null)
        {
            var ips = forwardedFor.Split(',');
            Address = new IPEndPoint(IPAddress.Parse(ips[0]), EndPoint?.Port ?? 0);
        }
        else
        {
            Logger.Log("X-Forwarded-For header not found, using remote endpoint", LoggingTarget.Network, LogLevel.Warning);
            Address = EndPoint;
        }

        var auth = Headers["Authorization"];

        if (string.IsNullOrEmpty(auth))
        {
            message = "Missing auth token.";
            return false;
        }

        if (!login(auth, Address!.Address.ToString(), out var issue))
        {
            message = issue;
            return false;
        }

        var version = Headers["X-Version"];

        if (string.IsNullOrEmpty(version))
        {
            message = "Outdated game client.";
            return false;
        }

        if (version == "local development build")
            return true;

        version = version.TrimStart('v');

        if (!Version.TryParse(version, out var ver))
        {
            message = "Invalid game version.";
            return false;
        }

        if (ver < SupportedVersion)
        {
            message = "Outdated version. Please update your game client.";
            return false;
        }

        return true;
    }

    private bool login(string token, string ip, out string issue)
    {
        issue = "";
        var session = SessionHelper.Get(token);

        if (session == null)
        {
            issue = ResponseStrings.InvalidToken;
            return false;
        }

        if (!UserHelper.TryGet(session.UserID, out var user))
        {
            issue = ResponseStrings.TokenUserNotFound;
            return false;
        }

        UserID = user.ID;

        if (!string.IsNullOrEmpty(user.CountryCode))
            return true;

        var code = IpUtils.GetCountryCode(ip).Result;
        UserHelper.UpdateLocked(UserID, u => u.CountryCode = code);
        return true;
    }
}
