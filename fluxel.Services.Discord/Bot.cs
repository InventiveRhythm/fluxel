using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using fluxel.Components.IPC;
using fluxel.Config;
using fluxel.Models;
using fluxel.Models.Chat;
using fluxel.Services.Discord.Components;
using fluxel.Services.Discord.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Midori.Logging;

namespace fluxel.Services.Discord;

public class Bot : BackgroundService
{
    private readonly DiscordConfig config;
    private readonly Valkey valkey;
    private readonly IServiceProvider services;

    public DiscordClient? Client { get; private set; }
    private static List<ISlashCommand>? commands { get; set; }

    public Bot(IOptions<DiscordConfig> options, IServiceProvider services, Valkey valkey)
    {
        config = options.Value;
        this.services = services;
        this.valkey = valkey;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(config.BotToken))
            return;

        var build = DiscordClientBuilder.CreateDefault(config.BotToken, DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents);
        build.SetLogLevel(LogLevel.None);

        commands = new List<ISlashCommand>
        {
            /*new AchievementTestCommand(),
            new GroupsCommandGroup(),
            new MaintenanceCommand(),
            new MapSetCommand(),
            new ModdingInactiveCommand(),
            new RecalculateCommand(),
            new RegenerateCommandGroup(),
            new RunTaskCommand(),
            new UserCommand(),
            new UsersCommandGroup(),*/
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
                var db = svc.ServiceProvider.GetRequiredService<DatabaseContext>();
                var user = db.Users.ByDiscordID(args.Member.Id);
                if (user is null) return;

                await args.Member.GrantRoleAsync(args.Guild.GetRoleAsync(1541436614805299210).Result);
            });
        });

        Client = build.Build();
        await Client.ConnectAsync();
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
                    var db = svc.ServiceProvider.GetRequiredService<DatabaseContext>();
                    var user = db.Users.ByDiscordID(args.Interaction.User.Id);

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
        Logger.Log($"Logged in as {Client!.CurrentUser.Username}#{Client.CurrentUser.Discriminator}!");

        if (commands == null) throw new Exception("Commands are null!");

        await Client.BulkOverwriteGlobalApplicationCommandsAsync(commands.Select(x => x.Build()));
        await Client.UpdateStatusAsync(new DiscordActivity("fluXis", DiscordActivityType.Playing));
    }

    private async Task messageCreated(DiscordClient _, MessageCreatedEventArgs args)
    {
        if (args.Channel.Id != config.ChannelChat) return;
        if (args.Author.IsBot) return;

        if (string.IsNullOrWhiteSpace(args.Message.Content))
        {
            Logger.Log("No message content. Is the message content intent enabled?");
            return;
        }

        using var svc = services.CreateScope();
        using var database = svc.ServiceProvider.GetRequiredService<DatabaseContext>();

        var user = database.Users.ByDiscordID(args.Author.Id);

        if (user is null)
        {
            try
            {
                await args.Message.DeleteAsync();

                var member = await args.Guild.GetMemberAsync(args.Author.Id);
                var channel = await member.CreateDmChannelAsync();

                var sb = new StringBuilder();
                sb.AppendLine("Your message has not been sent because your discord account is not linked to any fluXis account.");
                sb.AppendLine("Launch the game while having discord open and link your account using the popup in discord.");
                await channel.SendMessageAsync(new DiscordMessageBuilder().WithContent(sb.ToString()));
            }
            catch { }

            return;
        }

        var message = database.ChatMessages.Add(new ChatMessage("general", user.ID, args.Message.Content)).Entity;
        await database.SaveChangesAsync();

        await valkey.Publish(Valkey.ChatMessageCreate, new ValkeyStringIDEvent(message.ID));
    }

    private async Task messageDeleted(DiscordClient _, MessageDeletedEventArgs args)
    {
        if (args.Channel.Id != config.ChannelChat)
            return;

        using var svc = services.CreateScope();
        using var database = svc.ServiceProvider.GetRequiredService<DatabaseContext>();

        var message = database.ChatMessages.FirstOrDefault(x => x.DiscordID == args.Message.Id);
        if (message is null) return;

        using (database.EditAndSave())
            message.DeletedAt = DateTime.UtcNow;

        await valkey.Publish(Valkey.ChatMessageDelete, new ValkeyStringIDEvent(message.ID));
    }

    private DiscordChannel? getChannel(ulong id) => Client?.GetChannelAsync(id).Result;

    public DiscordChannel? GetChannel(ChannelType type) => type switch
    {
        ChannelType.Registrations => getChannel(config.ChannelRegistrations),
        ChannelType.Logging => getChannel(config.ChannelLogging),
        ChannelType.MapSubmissions => getChannel(config.ChannelSubmissions),
        ChannelType.MapRanked => getChannel(config.ChannelRanked),
        ChannelType.Queue => getChannel(config.ChannelQueue),
        ChannelType.MapFirstPlace => getChannel(config.ChannelRecords),
        ChannelType.ChatLink => getChannel(config.ChannelChat),
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
