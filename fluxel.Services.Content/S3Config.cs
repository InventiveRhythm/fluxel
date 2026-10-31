namespace fluxel.Services.Content;

public class S3Config
{
    public string Url { get; init; } = "http://seaweedfs:8333";
    public string AccessKey { get; init; } = "fluxel";
    public string SecretKey { get; init; } = string.Empty;
}
