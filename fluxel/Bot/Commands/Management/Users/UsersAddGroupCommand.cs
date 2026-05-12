using System;
using System.Collections.Generic;
using DSharpPlus.Entities;
using fluxel.Bot.Components;
using fluxel.Bot.Utils;
using fluxel.Database;
using Microsoft.Extensions.DependencyInjection;

namespace fluxel.Bot.Commands.Management.Users;

public class UsersAddGroupCommand : ISlashCommand
{
    public string Name => "add-group";
    public string Description => "Add a group to a user.";

    public IEnumerable<ISlashCommand.Option> Options => new List<ISlashCommand.Option>
    {
        new(OptionType.Integer, "user", "The user to add the group to.", true),
        new(OptionType.String, "group", "The group to add to the user.", true)
    };

    public void Handle(DiscordInteraction interaction, IServiceProvider services)
    {
        var users = services.GetRequiredService<UserManager>();

        var userId = interaction.GetInt("user");
        var groupId = interaction.GetString("group");

        if (userId is null || groupId is null)
        {
            interaction.Reply("Invalid user or group.", true);
            return;
        }

        var user = users.Get(userId.Value);

        if (user is null)
        {
            interaction.Reply("User not found.", true);
            return;
        }

        var group = services.GetRequiredService<GroupManager>().Get(groupId);

        if (group is null)
        {
            interaction.Reply("Group not found.", true);
            return;
        }

        if (user.GroupIDs.Contains(group.ID))
        {
            interaction.Reply("User already has this group.", true);
            return;
        }

        users.UpdateLocked(user.ID, u => u.GroupIDs.Add(group.ID));
        interaction.Reply($"Added {user.Username} to {group.Name} ({group.Tag}).", true);
    }
}
