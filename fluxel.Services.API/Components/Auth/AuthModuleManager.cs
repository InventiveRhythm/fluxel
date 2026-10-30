using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace fluxel.Services.API.Components.Auth;

public class AuthModuleManager
{
    public Dictionary<string, IAuthMethod> AuthMethods { get; } = new();

    public AuthModuleManager(IServiceProvider services)
    {
        AuthMethods["steam"] = ActivatorUtilities.CreateInstance<SteamAuthMethod>(services);
        AuthMethods["legacy"] = ActivatorUtilities.CreateInstance<LegacyAuthMethod>(services);
    }

    public IAuthMethod? Find(string key) => AuthMethods.GetValueOrDefault(key);

    public bool TryFind(string key, [NotNullWhen(true)] out IAuthMethod? method)
    {
        method = Find(key);
        return method != null;
    }
}
