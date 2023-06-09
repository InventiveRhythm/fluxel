using Newtonsoft.Json;

namespace fluxel.Utils; 

public class IpUtils {
    public static async Task<string?> GetCountryCode(string ip) {
        try {
            using var client = new HttpClient();
            var json = await client.GetStringAsync($"http://ip-api.com/json/{ip}");
            var obj = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            return obj?["countryCode"].ToLowerInvariant();
        } catch {
            return null;
        }
    }
}