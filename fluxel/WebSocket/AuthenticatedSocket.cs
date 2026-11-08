using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using fluxel.Constants;
using fluxel.Database.Extensions;
using fluxel.Models;
using fluxel.Models.Users;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Midori.Logging;
using Midori.Networking.WebSockets.Typed;

namespace fluxel.WebSocket;

[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public abstract class AuthenticatedSocket<S, C> : TypedWebSocketSession<S, C>
    where S : class where C : class
{
    public virtual Version SupportedVersion => new(2026, 508);

    public IPEndPoint? Address { get; private set; }
    public long UserID { get; set; }

    public User CurrentUser
    {
        get
        {
            if (UserID <= 0)
                throw new InvalidOperationException("Tried to get user before login.");

            using var db = UseDatabase();
            return db.Users.Find(UserID) ?? throw new KeyNotFoundException("Failed to find the current user.");
        }
    }

    protected readonly IServiceProvider Services;

    protected AuthenticatedSocket(IServiceProvider services)
    {
        Services = services;
    }

    protected DatabaseContext UseDatabase(IServiceProvider? svc = null)
    {
        svc ??= Services.CreateScope().ServiceProvider;
        var db = svc.GetRequiredService<DatabaseContext>();
        return db;
    }

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
            message = "OUTDATED_CLIENT";
            return false;
        }

        if (version == "local development build")
            return true;

        version = version.TrimStart('v');

        if (!Version.TryParse(version, out var ver))
        {
            message = "INVALID_CLIENT";
            return false;
        }

        if (ver < SupportedVersion)
        {
            message = "OUTDATED_CLIENT";
            return false;
        }

        return true;
    }

    private bool login(string token, string ip, out string issue)
    {
        issue = "";

        using var db = UseDatabase();
        var session = db.AuthSessions.FirstOrDefault(x => x.Token == token);

        if (session == null)
        {
            issue = ResponseStrings.InvalidToken;
            return false;
        }

        if (!db.Users.TryFind(out var user, session.UserID))
        {
            issue = ResponseStrings.TokenUserNotFound;
            return false;
        }

        using (db.EditAndSave())
            session.LastActivity = DateTime.UtcNow;

        UserID = user.ID;

        if (!string.IsNullOrEmpty(user.CountryCode))
            return true;

        var loc = Utilities.GetIPInfo(ip).Result;
        using (db.EditAndSave()) user.CountryCode = loc.country;

        return true;
    }
}
