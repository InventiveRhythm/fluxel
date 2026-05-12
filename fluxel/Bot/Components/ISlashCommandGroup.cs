using System;
using System.Collections.Generic;
using System.Linq;
using DSharpPlus.Entities;
using fluxel.Bot.Utils;

namespace fluxel.Bot.Components;

public interface ISlashCommandGroup : ISlashCommand
{
    int Depth => 1;
    IEnumerable<ISlashCommand> Subcommands { get; }

    void ISlashCommand.Handle(DiscordInteraction interaction, IServiceProvider services)
    {
        var option = interaction.Data.Options.First();
        var subcommand = option.Name;

        for (var i = 0; i < Depth - 1; i++)
        {
            option = option.Options.First();
            subcommand = option.Name;
        }

        var command = Subcommands.FirstOrDefault(x => x.Name == subcommand);

        if (command is null)
        {
            interaction.Reply("Subcommand not found.", true);
            return;
        }

        command.Handle(interaction, services);
    }
}
