using System.Diagnostics.CodeAnalysis;
using fluxel.Models.Users;
using fluXis.Online.API.Payloads.Auth;
using Midori.Networking;

namespace fluxel.Services.API.Components.Auth;

public interface IAuthMethod
{
    bool TryAuthenticate(HttpServerContext ctx, LoginPayload payload, [NotNullWhen(true)] out User? user, [NotNullWhen(false)] out string? error);
}
