using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Midori.Logging;
using Newtonsoft.Json;
using osu.Framework.IO.Network;

namespace fluxel.Models;

public static class Utilities
{
    public static async Task<(string? country, string? region, string? city)> GetIPInfo(string ip)
    {
        if (ip is "127.0.0.1" or "::1")
            return (null, null, null);

        try
        {
            var req = new JsonWebRequest<IpAPIResponse>($"http://ip-api.com/json/{ip}");
            req.AllowInsecureRequests = true;
            await req.PerformAsync();

            var res = req.ResponseObject;
            return (
                res.CountryCode?.ToLowerInvariant(),
                res.RegionName?.ToLowerInvariant(),
                res.City?.ToLowerInvariant()
            );
        }
        catch
        {
            Logger.Log($"Failed to get country code for {ip}", LoggingTarget.Network, LogLevel.Error);
            return (null, null, null);
        }
    }

    private class IpAPIResponse
    {
        [JsonProperty("countryCode")]
        public string? CountryCode { get; set; }

        [JsonProperty("regionName")]
        public string? RegionName { get; set; }

        [JsonProperty("city")]
        public string? City { get; set; }
    }
}
