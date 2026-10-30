using System.Collections.Generic;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace fluxel.Services.API.Components;

public static class OpenID
{
    public static Dictionary<string, (long id, string client)> Codes { get; set; } = new();

    public static readonly RsaSecurityKey KEY;

    static OpenID()
    {
        var rsa = RSA.Create(2048);
        KEY = new RsaSecurityKey(rsa) { KeyId = "fluXis" };
    }
}
