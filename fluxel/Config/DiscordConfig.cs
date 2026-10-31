namespace fluxel.Config;

public class DiscordConfig
{
    public string BotToken { get; init; } = string.Empty;

    public ulong ClientID { get; init; }
    public string ClientSecret { get; init; } = string.Empty;

    public ulong ChannelChat { get; init; }
    public ulong ChannelLogging { get; init; }
    public ulong ChannelQueue { get; init; }
    public ulong ChannelRanked { get; init; }
    public ulong ChannelRecords { get; init; }
    public ulong ChannelRegistrations { get; init; }
    public ulong ChannelSubmissions { get; init; }
}
