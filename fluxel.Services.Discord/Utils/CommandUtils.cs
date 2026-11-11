using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using fluxel.Services.Discord.Components;

namespace fluxel.Services.Discord.Utils;

public static class CommandUtils
{
    // all of this is just taken from Snowly

    public static DiscordApplicationCommand Build(this ISlashCommand command)
    {
        DiscordApplicationCommand appCommand;

        switch (command)
        {
            case ISlashCommandGroup group:
                appCommand = new DiscordApplicationCommand(
                    command.Name,
                    command.Description,
                    group.Subcommands.Select(buildSubcommand),
                    defaultMemberPermissions: command.Permissions);
                break;

            default:
                appCommand = new DiscordApplicationCommand(
                    command.Name,
                    command.Description,
                    command.Options.Select(buildOption),
                    defaultMemberPermissions: command.Permissions);
                break;
        }

        return appCommand;
    }

    private static DiscordApplicationCommandOption buildOption(ISlashCommand.Option option)
    {
        return new DiscordApplicationCommandOption(
            option.Name,
            option.Description,
            option.Type.convert(),
            option.Required,
            option.Choices.Select(x => new DiscordApplicationCommandOptionChoice(x.Name, x.Value as string ?? "what"))
        );
    }

    private static DiscordApplicationCommandOption buildSubcommand(ISlashCommand subCommand, int depth = 0)
    {
        DiscordApplicationCommandOption builder;

        switch (subCommand)
        {
            case ISlashCommandGroup group:
                builder = new DiscordApplicationCommandOption(
                    subCommand.Name,
                    subCommand.Description,
                    DiscordApplicationCommandOptionType.SubCommandGroup,
                    null, Array.Empty<DiscordApplicationCommandOptionChoice>(),
                    group.Subcommands.Select(s => buildSubcommand(s, depth + 1)));
                break;

            default:
                builder = new DiscordApplicationCommandOption(
                    subCommand.Name,
                    subCommand.Description,
                    DiscordApplicationCommandOptionType.SubCommand,
                    null, Array.Empty<DiscordApplicationCommandOptionChoice>(),
                    subCommand.Options.Select(buildOption));

                break;
        }

        return builder;
    }

    private static DiscordApplicationCommandOptionType convert(this OptionType type)
    {
        return type switch
        {
            OptionType.String => DiscordApplicationCommandOptionType.String,
            OptionType.Integer => DiscordApplicationCommandOptionType.Integer,
            OptionType.Boolean => DiscordApplicationCommandOptionType.Boolean,
            OptionType.User => DiscordApplicationCommandOptionType.User,
            OptionType.Channel => DiscordApplicationCommandOptionType.Channel,
            OptionType.Role => DiscordApplicationCommandOptionType.Role,
            _ => DiscordApplicationCommandOptionType.String
        };
    }

    #region Reply

    public static void ReplyEmbed(this DiscordInteraction interaction, DiscordEmbedBuilder embed, bool ephemeral = false)
    {
        interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder
        {
            IsEphemeral = ephemeral
        }.AddEmbed(embed.Build()));
    }

    public static void Reply(this DiscordInteraction interaction, string content, bool ephemeral = false)
    {
        interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder
        {
            IsEphemeral = ephemeral,
            Content = content
        });
    }

    public static void ReplyAutoComplete(this DiscordInteraction interaction, IEnumerable<DiscordAutoCompleteChoice> choices)
    {
        var response = new DiscordInteractionResponseBuilder();
        response.AddAutoCompleteChoices(choices);
        interaction.CreateResponseAsync(DiscordInteractionResponseType.AutoCompleteResult, response);
    }

    #endregion

    #region Acknowledge

    public static async Task Acknowledge(this DiscordInteraction interaction)
    {
        await interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredChannelMessageWithSource);
    }

    public static async Task AcknowledgeEphemeral(this DiscordInteraction interaction)
    {
        await interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder
        {
            IsEphemeral = true
        });
    }

    #endregion

    #region Followup

    public static void FollowupEmbed(this DiscordInteraction interaction, DiscordEmbedBuilder embed, bool ephemeral = false)
    {
        interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder
        {
            IsEphemeral = ephemeral
        }.AddEmbed(embed.Build()));
    }

    public static void Followup(this DiscordInteraction interaction, string content, bool ephemeral = false)
    {
        interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder
        {
            IsEphemeral = ephemeral,
            Content = content
        });
    }

    #endregion

    #region Options

    public static string? GetString(this DiscordInteraction interaction, string name)
    {
        return interaction.getOptions()?.Where(option => option.Name == name).Select(option => option.Value).FirstOrDefault() as string;
    }

    public static long? GetInt(this DiscordInteraction interaction, string name)
    {
        var value = interaction.getOptions()?.Where(option => option.Name == name).Select(option => option.Value).FirstOrDefault();
        return value is not long number ? null : number;
    }

    public static bool? GetBool(this DiscordInteraction interaction, string name)
    {
        var opt = interaction.getOptions()?.Where(option => option.Name == name);
        return opt?.Select(option => option.Value).FirstOrDefault() as bool?;
    }

    public static async Task<DiscordUser?> GetUser(this DiscordInteraction interaction, Bot bot, string name)
    {
        var value = interaction.getOptions()?.Where(option => option.Name == name).Select(option => option.Value).FirstOrDefault();
        if (value is not ulong id) return null;

        return await bot.Client!.GetUserAsync(id);
    }

    public static async Task<DiscordUser?> GetMember(this DiscordInteraction interaction, string name)
    {
        var value = interaction.getOptions()?.Where(option => option.Name == name).Select(option => option.Value).FirstOrDefault();
        if (value is not ulong id) return null;

        return await interaction.Guild.GetMemberAsync(id);
    }

    public static DiscordChannel? GetChannel(this DiscordInteraction interaction, string name)
    {
        var value = interaction.getOptions()?.Where(option => option.Name == name).Select(option => option.Value).FirstOrDefault();
        return value is not ulong id ? null : interaction.Guild?.GetChannelAsync(id).Result;
    }

    public static DiscordRole? GetRole(this DiscordInteraction interaction, string name)
    {
        var value = interaction.getOptions()?.Where(option => option.Name == name).Select(option => option.Value).FirstOrDefault();
        return value is not ulong id ? null : interaction.Guild?.GetRoleAsync(id).Result;
    }

    public static DiscordAttachment? GetAttachment(this DiscordInteraction interaction, string name)
    {
        var id = (ulong)(interaction.getOptions()?.Where(option => option.Name == name).Select(o => o.Value).FirstOrDefault() ?? 0);
        return id == 0 ? null : interaction.Data.Resolved?.Attachments?.Where(a => a.Key == id).Select(b => b.Value).FirstOrDefault();
    }

    private static IEnumerable<DiscordInteractionDataOption>? getOptions(this DiscordInteraction interaction)
    {
        var options = interaction.Data?.Options?.ToList();
        if (options == null || !options.Any()) return null;

        while (options.FirstOrDefault()?.Options?.Any() ?? false)
        {
            options = options.First().Options.ToList();
        }

        return options;
    }

    #endregion
}
