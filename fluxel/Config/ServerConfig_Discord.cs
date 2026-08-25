namespace fluxel.Config;

public partial class ServerConfig
{
    public class DiscordConfig
    {
        public string Token { get; init; } = string.Empty;

        public string ClientID { get; init; } = string.Empty;
        public string ClientSecret { get; init; } = string.Empty;

        /// <summary>
        /// Used for logging stuff like role changes, etc.
        /// </summary>
        public ulong Logging { get; init; }

        /// <summary>
        /// Used for logging registrations.
        /// </summary>
        public ulong Registrations { get; init; }

        /// <summary>
        /// Used when a map gets uploaded.
        /// </summary>
        public ulong MapSubmissions { get; init; }

        /// <summary>
        /// Used when a map gets ranked.
        /// </summary>
        public ulong MapRanked { get; init; }

        /// <summary>
        /// Updates for the modding queue.
        /// </summary>
        public ulong QueueUpdates { get; init; }

        /// <summary>
        /// When someone gets first place on a map.
        /// </summary>
        public ulong MapFirstPlace { get; init; }

        /// <summary>
        /// Links the #general chat channel and sends messages both ways
        /// </summary>
        public ulong ChatLink { get; init; }
    }
}
