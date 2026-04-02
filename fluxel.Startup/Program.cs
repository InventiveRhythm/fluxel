using DotNetEnv;
using fluxel.Config;

namespace fluxel.Startup;

internal static class Program
{
    private static async Task Main()
    {
        Env.Load();

        var config = new ServerConfig
        {
            Port = Env.GetInt("PORT"),
            FfmpegPath = Env.GetString("FFMPEG_PATH"),
            KoFiSecret = Env.GetString("KOFI_SECRET"),
            BundledSets = envLongList("BUNDLED_SETS").ToArray(),
            Limits = new ServerConfig.LimitsConfig
            {
                MaxMapSets = Env.GetInt("LIMITS_MAX_MAPSETS"),
                IncreasePerPure = Env.GetInt("LIMITS_INCREASE_PER_PURE")
            },
            Mongo = new ServerConfig.MongoConfig
            {
                Connection = Env.GetString("MONGO_CONN"),
                Database = Env.GetString("MONGO_DB"),
            },
            Steam = new ServerConfig.SteamConfig
            {
                AppID = (uint)Env.GetInt("STEAM_APPID"),
                WebKey = Env.GetString("STEAM_WEBKEY")
            },
            Urls = new ServerConfig.UrlConfig
            {
                Website = Env.GetString("URL_WEB"),
                Assets = Env.GetString("URL_ASSETS"),
            },
            Discord = new ServerConfig.DiscordConfig
            {
                Token = Env.GetString("DISCORD_TOKEN"),
                Logging = envULong("DISCORD_LOGGING"),
                Registrations = envULong("DISCORD_REGISTER"),
                MapSubmissions = envULong("DISCORD_SUBMISSIONS"),
                MapFirstPlace = envULong("DISCORD_FIRST_PLACE"),
                MapRanked = envULong("DISCORD_RANKED"),
                QueueUpdates = envULong("DISCORD_QUEUE"),
                ChatLink = envULong("DISCORD_CHAT_LINK")
            },
            Mail = new ServerConfig.MailConfig
            {
                Host = Env.GetString("MAIL_HOST"),
                Port = Env.GetInt("MAIL_PORT"),
                Username = Env.GetString("MAIL_USER"),
                Password = Env.GetString("MAIL_PASS"),
                Name = Env.GetString("MAIL_NAME")
            },
            MailFlux = new ServerConfig.MailConfig
            {
                Host = Env.GetString("MAIL_DONO_HOST"),
                Port = Env.GetInt("MAIL_DONO_PORT"),
                Username = Env.GetString("MAIL_DONO_USER"),
                Password = Env.GetString("MAIL_DONO_PASS"),
                Name = Env.GetString("MAIL_DONO_NAME")
            }
        };

        var server = new ServerHost(config);
        await server.StartBlocking();
    }

    private static ulong envULong(string key)
    {
        var var = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrWhiteSpace(var)) return 0;

        if (ulong.TryParse(var, out var ul))
            return ul;

        return 0;
    }

    private static List<long> envLongList(string key)
    {
        var var = Env.GetString(key);
        if (string.IsNullOrWhiteSpace(var)) return [];

        var split = var.Split(",", StringSplitOptions.RemoveEmptyEntries);
        var nums = new List<long>();

        foreach (var se in split)
        {
            if (!long.TryParse(se, out var n))
                return [];

            nums.Add(n);
        }

        return nums;
    }
}
