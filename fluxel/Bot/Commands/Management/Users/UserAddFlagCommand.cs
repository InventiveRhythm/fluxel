using System;
using System.Collections.Generic;
using System.Linq;
using DSharpPlus.Entities;
using fluxel.Bot.Components;
using fluxel.Bot.Utils;
using fluxel.Database;
using fluxel.Models.Users;
using Microsoft.Extensions.DependencyInjection;
using osu.Framework.Extensions;

namespace fluxel.Bot.Commands.Management.Users;

public class UserAddFlagCommand : ISlashCommand
{
    public string Name => "add-flag";
    public string Description => "Add a flag to a user.";

    public IEnumerable<ISlashCommand.Option> Options => new List<ISlashCommand.Option>
    {
        new(OptionType.Integer, "user", "The user to add the flag to.", true),
        new ISlashCommand.Option(OptionType.String, "flag", "The flag to add.", true)
            .WithChoices(getFlagChoices().ToArray())
    };

    public async void Handle(DiscordInteraction interaction, IServiceProvider services)
    {
        var users = services.GetRequiredService<UserManager>();

        var userId = interaction.GetInt("user");
        var flag = interaction.GetString("flag");

        if (userId is null || flag is null)
        {
            interaction.Reply("Invalid user or flag.", true);
            return;
        }

        if (!users.TryGet(userId.Value, out var user))
        {
            interaction.Reply("User not found.", true);
            return;
        }

        if (!Enum.TryParse<UserBanFlag>(flag, true, out var userFlag))
        {
            interaction.Reply("Invalid flag.", true);
            return;
        }

        if (user.BanFlags.HasFlag(userFlag))
        {
            interaction.Reply("User already has this flag.", true);
            return;
        }

        users.UpdateLocked(user.ID, u => u.BanFlags |= userFlag);
        interaction.Reply($"Flag **{userFlag.ToString()}** added to user **{user.Username}**.", true);
    }

    private static IEnumerable<ISlashCommand.Choice> getFlagChoices()
    {
        var flags = Enum.GetValues<UserBanFlag>();
        var choices = new List<ISlashCommand.Choice>();

        foreach (var flag in flags)
            choices.Add(new ISlashCommand.Choice(flag.GetDescription(), flag.ToString()));

        return choices;
    }
}
