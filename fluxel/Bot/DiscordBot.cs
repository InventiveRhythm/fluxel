using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace fluxel.Bot;

public static class DiscordBot
{
    public static DiscordClient Bot { get; private set; } = null!;

    public static void Start()
    {
        if (Bot != null) throw new Exception("Bot is already running!");

        Bot = new DiscordClient(new DiscordConfiguration
        {
            Token = Program.Config.Token,
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.AllUnprivileged,
            AutoReconnect = true,
            MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.None
        });

        Bot.Ready += ready;
        Bot.ConnectAsync().Wait();
    }

    private static async Task ready(DiscordClient sender, ReadyEventArgs args)
    {
        Logger.Log($"Logged in as {Bot.CurrentUser.Username}#{Bot.CurrentUser.Discriminator}!");

        await Bot.UpdateStatusAsync(new DiscordActivity("fluXis", ActivityType.Playing));

        GetLoggingChannel()?.SendMessageAsync(new DiscordMessageBuilder { Content = "Discord bot started!" });
    }

    public static DiscordChannel? GetLoggingChannel() => Bot.GetChannelAsync(Program.Config.LoggingChannelId).Result;
}
