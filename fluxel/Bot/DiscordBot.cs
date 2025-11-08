using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using fluxel.Bot.Commands;
using fluxel.Bot.Commands.Management;
using fluxel.Bot.Commands.Messages;
using fluxel.Bot.Commands.Testing;
using fluxel.Bot.Components;
using fluxel.Bot.Utils;
using fluxel.Config;
using Midori.Logging;

namespace fluxel.Bot;

public static class DiscordBot
{
    private static ServerConfig.DiscordConfig config = null!;

    public static DiscordClient? Bot { get; private set; }
    private static List<ISlashCommand>? commands { get; set; }

    public static async Task StartAsync(ServerConfig.DiscordConfig config)
    {
        if (Bot != null)
            throw new Exception("Bot is already running!");

        DiscordBot.config = config;

        if (string.IsNullOrWhiteSpace(config.Token))
            return;

        Bot = new DiscordClient(new DiscordConfiguration
        {
            Token = config.Token,
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.AllUnprivileged,
            AutoReconnect = true,
            MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.None
        });

        commands = new List<ISlashCommand>
        {
            new AchievementTestCommand(),
            new GroupsCommandGroup(),
            new ImageMessageCommand(),
            new MaintenanceCommand(),
            new MessageCommand(),
            new MapSetCommand(),
            new ModdingInactiveCommand(),
            new RecalculateCommand(),
            new RegenerateCommandGroup(),
            new RunTaskCommand(),
            new UserCommand(),
            new UsersCommandGroup(),
        };

        Bot.Ready += ready;
        Bot.InteractionCreated += onInteraction;
        await Bot.ConnectAsync();
    }

    public static async Task Stop()
    {
        if (Bot is null) return;

        Logger.Log("Shutting down Discord bot.");
        await Bot.DisconnectAsync();
    }

    private static async Task onInteraction(DiscordClient sender, InteractionCreateEventArgs args)
    {
        var command = commands!.FirstOrDefault(x => x.Name == args.Interaction.Data.Name);

        if (command == null)
        {
            await args.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Command not found!").AsEphemeral());
            return;
        }

        try
        {
            command.Handle(args.Interaction);
        }
        catch (Exception e)
        {
            args.Interaction.Reply("An error occurred while executing the command!", true);
            Logger.Error(e, "An error occurred while executing a command!");
        }
    }

    private static async Task ready(DiscordClient sender, ReadyEventArgs args)
    {
        Logger.Log($"Logged in as {Bot!.CurrentUser.Username}#{Bot.CurrentUser.Discriminator}!");

        if (commands == null) throw new Exception("Commands are null!");

        await Bot.BulkOverwriteGlobalApplicationCommandsAsync(commands.Select(x => x.Build()));
        await Bot.UpdateStatusAsync(new DiscordActivity("fluXis", ActivityType.Playing));
    }

    private static DiscordChannel? getChannel(ulong id) => Bot?.GetChannelAsync(id).Result;

    public static DiscordChannel? GetChannel(ChannelType type) => type switch
    {
        ChannelType.Registrations => getChannel(config.Registrations),
        ChannelType.Logging => getChannel(config.Logging),
        ChannelType.MapSubmissions => getChannel(config.MapSubmissions),
        ChannelType.MapRanked => getChannel(config.MapRanked),
        ChannelType.Queue => getChannel(config.QueueUpdates),
        ChannelType.MapFirstPlace => getChannel(config.MapFirstPlace),
        _ => null
    };

    public static void SendException(Exception e)
    {
        var channel = GetChannel(ChannelType.Logging);

        var stackTrace = e.StackTrace?[..Math.Min(e.StackTrace.Length, 1000)];

        var message = new DiscordMessageBuilder()
                      .WithContent("<@386436194709274627>")
                      .WithAllowedMention(new UserMention(386436194709274627))
                      .WithEmbed(new DiscordEmbedBuilder()
                                 .WithTitle("Unhandled exception")
                                 .WithDescription(e.Message)
                                 .AddField("Stack trace", $"```{stackTrace ?? "No stack trace"}```")
                                 .WithColor(DiscordColor.Red));

        channel?.SendMessageAsync(message);
    }

    public enum ChannelType
    {
        Logging,
        Registrations,
        MapSubmissions,
        MapRanked,
        Queue,
        MapFirstPlace
    }
}
