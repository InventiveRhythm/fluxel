namespace fluxel.Config;

public partial class ServerConfig
{
    public required int Port { get; init; }
    public string AssetsPath { get; init; } = string.Empty;

    public required string ValkeyConnection { get; init; }

    public required string MeilisearchUrl { get; init; }
    public required string MeilisearchKey { get; init; }

    public required string FfmpegPath { get; init; }
    public required string KoFiSecret { get; init; }
    public required long[] BundledSets { get; init; }

    public LimitsConfig Limits { get; init; } = new();
    public UrlConfig Urls { get; init; } = new();
}
