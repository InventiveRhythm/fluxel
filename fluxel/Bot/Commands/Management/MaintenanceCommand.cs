using System;
using System.Collections.Generic;
using DSharpPlus.Entities;
using fluxel.Bot.Components;
using fluxel.Bot.Utils;

namespace fluxel.Bot.Commands.Management;

public class MaintenanceCommand : ISlashCommand
{
    public string Name => "maintenance";
    public string Description => "Start a maintenance countdown";

    public IEnumerable<ISlashCommand.Option> Options => new List<ISlashCommand.Option>
    {
        new(OptionType.Integer, "time", "Unix timestamp of the time the server will go down", true)
    };

    public void Handle(DiscordInteraction interaction, IServiceProvider services)
    {
        var time = interaction.GetInt("time");

        if (time == null)
        {
            interaction.Reply("Invalid time", true);
            return;
        }

        if (time < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        {
            interaction.Reply("The time must be in the future", true);
            return;
        }

        Program.StartMaintenanceCountdown(time.Value);
        interaction.Reply("Maintenance countdown started", true);
    }
}
