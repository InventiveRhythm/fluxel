using System;

namespace fluxel.Config;

public partial class ServerConfig
{
    public int Port { get; init; } = 2434;
    public string ValkeyConnection { get; init; } = string.Empty;
    public string FfmpegPath { get; init; } = string.Empty;
    public string KoFiSecret { get; init; } = string.Empty;
    public long[] BundledSets { get; init; } = Array.Empty<long>();

    public LimitsConfig Limits { get; init; } = new();
    public MongoConfig Mongo { get; init; } = new();
    public SteamConfig Steam { get; init; } = new();
    public UrlConfig Urls { get; init; } = new();
    public DiscordConfig Discord { get; init; } = new();
    public MailConfig Mail { get; init; } = new();
    public MailConfig MailFlux { get; init; } = new();
}
