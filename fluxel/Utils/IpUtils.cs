using Newtonsoft.Json;

namespace fluxel.Utils; 

public class IpUtils {
    public static async Task<string?> GetCountryCode(string ip) {
        try {
            using var client = new HttpClient();
            var json = await client.GetStringAsync($"http://ip-api.com/json/{ip}");
            var obj = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            Console.WriteLine(json);
            
            var code = obj?["countryCode"].ToLowerInvariant();
            
            if (code == null) {
                Console.WriteLine($"Failed to get country code for {ip}");
            }
            
            return code;
        } catch (Exception e) {
            Console.WriteLine($"Failed to get country code for {ip}");
            Console.WriteLine(e);
            return null;
        }
    }
}