using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using fluxel.Models;
using fluxel.Models.Auth;
using fluxel.Models.Users;
using fluXis.Online.API.Payloads.Auth;
using Microsoft.Extensions.DependencyInjection;
using Midori.Networking;

namespace fluxel.Services.API.Components.Auth;

public class LegacyAuthMethod : IAuthMethod
{
    private readonly IServiceProvider services;

    public LegacyAuthMethod(IServiceProvider services)
    {
        this.services = services;
    }

    public bool TryAuthenticate(HttpServerContext ctx, LoginPayload payload, [NotNullWhen(true)] out User? user, [NotNullWhen(false)] out string? error)
    {
        using (var scope = services.CreateScope())
        {
            user = null;
            error = null;

            var username = payload.Data.GetValue("username")?.ToString();
            var password = payload.Data.GetValue("password")?.ToString();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                error = "Username and password must not be empty.";
                return false;
            }

            var database = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

            var method = database.AuthInfos.OfType<LegacyAuthInfo>()
                                 .FirstOrDefault(x => x.Email == username);

            // attempt by username
            if (method is null)
            {
                user = database.Users.ByUsername(username);

                if (user is null)
                {
                    error = "No user with that username/e-mail.";
                    return false;
                }

                var id = user.ID;
                method = database.AuthInfos.OfType<LegacyAuthInfo>()
                                 .FirstOrDefault(x => x.UserID == id);
            }

            if (method is null)
            {
                error = "The selected login method is not valid for this user.";
                return false;
            }

            if (!BCrypt.Net.BCrypt.Verify(password, method.Password))
            {
                error = "The provided password is incorrect.";
                return false;
            }

            user ??= database.Users.Find(method.UserID);

            if (user is null)
            {
                error = "Login success but user does not exist.";
                return false;
            }

            return true;
        }
    }
}
