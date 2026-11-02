using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using fluxel.Bot.Commands;
using fluxel.Bot.Commands.Management;
using fluxel.Bot.Commands.Testing;
using fluxel.Bot.Components;
using fluxel.Bot.Utils;
using fluxel.Config;
using fluxel.Database;
using fluxel.Modules;
using fluxel.Modules.Messages.Chat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Midori.Logging;

namespace fluxel.Bot;

public class DiscordBot : BackgroundService
{
    private readonly ServerConfig.DiscordConfig config;
    private readonly ModuleManager modules;
    private readonly IServiceProvider services;

    public DiscordClient? Bot { get; private set; }
    private static List<ISlashCommand>? commands { get; set; }

    public DiscordBot(ServerConfig config, ModuleManager modules, IServiceProvider services)
    {
        this.modules = modules;
        this.services = services;
        this.config = config.Discord;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(config.Token))
            return;

        var build = DiscordClientBuilder.CreateDefault(config.Token, DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents);
        build.SetLogLevel(LogLevel.None);

        commands = new List<ISlashCommand>
        {
            new AchievementTestCommand(),
            new GroupsCommandGroup(),
            new MaintenanceCommand(),
            new MapSetCommand(),
            new ModdingInactiveCommand(),
            new RecalculateCommand(),
            new RegenerateCommandGroup(),
            new RunTaskCommand(),
            new UserCommand(),
            new UsersCommandGroup(),
            new SendVerifyMessageCommand()
        };

        build.ConfigureEventHandlers(c =>
        {
            c.HandleSessionCreated(ready);
            c.HandleInteractionCreated(onInteraction);
            c.HandleMessageCreated(messageCreated);
            c.HandleMessageDeleted(messageDeleted);
            c.HandleGuildMemberAdded(async (client, args) =>
            {
                using var svc = services.CreateScope();
                var db = svc.ServiceProvider.GetRequiredService<UserManager>();
                var user = db.GetByDiscordID(args.Member.Id);
                if (user is null) return;

                await args.Member.GrantRoleAsync(args.Guild.GetRoleAsync(1541436614805299210).Result);
            });
        });

        Bot = build.Build();
        await Bot.ConnectAsync();
    }

    private async Task onInteraction(DiscordClient sender, InteractionCreatedEventArgs args)
    {
        if (args.Interaction.Type == DiscordInteractionType.Component)
        {
            switch (args.Interaction.Data.CustomId)
            {
                case "user-verify":
                {
                    var svc = services.CreateScope();
                    var db = svc.ServiceProvider.GetRequiredService<UserManager>();
                    var user = db.GetByDiscordID(args.Interaction.User.Id);

                    if (user is null)
                    {
                        args.Interaction.Reply("You have no fluXis account linked to your discord account!", true);
                        return;
                    }

                    var member = args.Interaction.Guild!.GetMemberAsync(args.Interaction.User.Id).Result;
                    member.GrantRoleAsync(args.Interaction.Guild.GetRoleAsync(1541436614805299210).Result).Wait();

                    args.Interaction.Reply("Done!", true);
                    break;
                }

                default:
                    Logger.Log($"Unknown component: {args.Interaction.Data.CustomId}.");
                    break;
            }

            return;
        }

        var command = commands!.FirstOrDefault(x => x.Name == args.Interaction.Data.Name);

        if (command == null)
        {
            await args.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("Command not found!").AsEphemeral());
            return;
        }

        try
        {
            command.Handle(args.Interaction, services);
        }
        catch (Exception e)
        {
            args.Interaction.Reply("An error occurred while executing the command!", true);
            Logger.Error(e, "An error occurred while executing a command!");
        }
    }

    private async Task ready(DiscordClient sender, SessionCreatedEventArgs sessionCreatedEventArgs)
    {
        Logger.Log($"Logged in as {Bot!.CurrentUser.Username}#{Bot.CurrentUser.Discriminator}!");

        if (commands == null) throw new Exception("Commands are null!");

        await Bot.BulkOverwriteGlobalApplicationCommandsAsync(commands.Select(x => x.Build()));
        await Bot.UpdateStatusAsync(new DiscordActivity("fluXis", DiscordActivityType.Playing));
    }

    private Task messageCreated(DiscordClient _, MessageCreatedEventArgs args)
    {
        if (args.Channel.Id != config.ChatLink)
            return Task.CompletedTask;
        if (args.Author.IsBot)
            return Task.CompletedTask;

        if (string.IsNullOrWhiteSpace(args.Message.Content))
        {
            Logger.Log("No message content. Is the message content intent enabled?");
            return Task.CompletedTask;
        }

        var svc = services.CreateScope();
        var users = svc.ServiceProvider.GetRequiredService<UserManager>();
        var chat = svc.ServiceProvider.GetRequiredService<ChatManager>();

        var user = users.GetByDiscordID(args.Author.Id);

        if (user is null)
        {
            try
            {
                args.Message.DeleteAsync();

                var member = args.Guild.GetMemberAsync(args.Author.Id).Result;
                var channel = member.CreateDmChannelAsync().Result;

                var sb = new StringBuilder();
                sb.AppendLine("Your message has not been sent because your discord account is not linked to any fluXis account.");
                sb.AppendLine("Launch the game while having discord open and link your account using the popup in discord.");
                channel.SendMessageAsync(new DiscordMessageBuilder().WithContent(sb.ToString()));
            }
            catch { }

            return Task.CompletedTask;
        }

        var message = chat.Add(user.ID, args.Message.Content, "general", args.Message.Id);
        modules.SendMessage(new ChatMessageCreateMessage(message.ID));

        return Task.CompletedTask;
    }

    private Task messageDeleted(DiscordClient _, MessageDeletedEventArgs args)
    {
        if (args.Channel.Id != config.ChatLink)
            return Task.CompletedTask;

        var svc = services.CreateScope();
        var chat = svc.ServiceProvider.GetRequiredService<ChatManager>();

        var message = chat.GetByDiscordID(args.Message.Id);
        if (message is null) return Task.CompletedTask;

        chat.Delete(message);
        modules.SendMessage(new ChatMessageDeleteMessage(message.ID));
        return Task.CompletedTask;
    }

    private DiscordChannel? getChannel(ulong id) => Bot?.GetChannelAsync(id).Result;

    public DiscordChannel? GetChannel(ChannelType type) => type switch
    {
        ChannelType.Registrations => getChannel(config.Registrations),
        ChannelType.Logging => getChannel(config.Logging),
        ChannelType.MapSubmissions => getChannel(config.MapSubmissions),
        ChannelType.MapRanked => getChannel(config.MapRanked),
        ChannelType.Queue => getChannel(config.QueueUpdates),
        ChannelType.MapFirstPlace => getChannel(config.MapFirstPlace),
        ChannelType.ChatLink => getChannel(config.ChatLink),
        _ => null
    };

    public void SendException(Exception e)
    {
        var channel = GetChannel(ChannelType.Logging);

        var stackTrace = e.StackTrace?[..Math.Min(e.StackTrace.Length, 1000)];

        var message = new DiscordMessageBuilder()
                      .WithContent("<@386436194709274627>")
                      .WithAllowedMention(new UserMention(386436194709274627))
                      .AddEmbed(new DiscordEmbedBuilder()
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
        MapFirstPlace,
        ChatLink
    }
}
