namespace fluxel.Config;

public class MailConfig
{
    /// <summary>
    /// SMTP server to use.
    /// </summary>
    public string Host { get; init; } = string.Empty;

    /// <summary>
    /// SMTP port to use.
    /// </summary>
    public int Port { get; init; } = 587;

    /// <summary>
    /// Username for the SMTP server.
    /// </summary>
    public string Username { get; init; } = string.Empty;

    /// <summary>
    /// Password for the SMTP server.
    /// </summary>
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// Name to send emails from.
    /// </summary>
    public string Name { get; init; } = string.Empty;
}
