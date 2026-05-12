using System;
using System.Collections.Generic;
using DSharpPlus.Entities;
using fluxel.Bot.Components;
using fluxel.Bot.Utils;
using fluxel.Database;
using fluxel.Models.Groups;
using Microsoft.Extensions.DependencyInjection;

namespace fluxel.Bot.Commands.Management.Groups;

public class GroupsAddCommand : ISlashCommand
{
    public string Name => "add";
    public string Description => "Add a group.";

    public IEnumerable<ISlashCommand.Option> Options => new List<ISlashCommand.Option>
    {
        new(OptionType.String, "id", "The ID of the group.", true),
        new(OptionType.String, "name", "The name of the group.", true),
        new(OptionType.String, "tag", "The tag of the group.", true),
        new(OptionType.String, "color", "The color of the group in hex. (#rrggbb)", false)
    };

    public void Handle(DiscordInteraction interaction, IServiceProvider services)
    {
        var id = interaction.GetString("id");
        var name = interaction.GetString("name");
        var tag = interaction.GetString("tag");
        var color = interaction.GetString("color");

        if (id is null || name is null || tag is null || color is null)
        {
            interaction.Reply("Missing required fields.", true);
            return;
        }

        var group = new Group
        {
            ID = id,
            Name = name,
            Tag = tag,
            Color = color
        };

        services.GetRequiredService<GroupManager>().Add(group);
        interaction.Reply($"Added group {name} ({tag}).", true);
    }
}
