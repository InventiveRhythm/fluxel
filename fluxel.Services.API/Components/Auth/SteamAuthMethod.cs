using System;
using System.Diagnostics.CodeAnalysis;
using fluxel.Config;
using fluxel.Models;
using fluxel.Models.Users;
using fluxel.Utils.Steam;
using fluXis.Online.API.Payloads.Auth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Midori.Networking;

namespace fluxel.Services.API.Components.Auth;

public class SteamAuthMethod : IAuthMethod
{
    private readonly SteamConfig config;
    private readonly IServiceProvider services;

    public SteamAuthMethod(IServiceProvider services, IOptions<SteamConfig> config)
    {
        this.services = services;
        this.config = config.Value;
    }

    public bool TryAuthenticate(HttpServerContext ctx, LoginPayload payload, [NotNullWhen(true)] out User? user, [NotNullWhen(false)] out string? error)
    {
        user = null;
        error = null;

        var ticket = payload.Data.GetValue("ticket")?.ToString();

        if (string.IsNullOrWhiteSpace(ticket))
        {
            error = "Ticket must not be empty.";
            return false;
        }

        ulong id;

        try
        {
            id = SteamHelper.FetchUserIDFromTicket(config, ticket).Result;
        }
        catch (Exception)
        {
            error = "Failed to get SteamID from ticket.";
            return false;
        }

        using var scope = services.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        user = db.Users.BySteamID(id);

        if (user is not null)
            return true;

        error = "This steam account has no user associated with it.";
        return false;
    }
}
