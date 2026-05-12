using System;
using System.Collections.Generic;
using DSharpPlus.Entities;
using fluxel.Bot.Components;
using fluxel.Bot.Utils;
using fluxel.Components;
using fluxel.Constants;
using fluxel.Database;
using fluxel.Models.Maps;
using fluxel.Tasks;
using fluxel.Tasks.Other;
using fluXis.Online.API.Models.Maps.Modding;
using Microsoft.Extensions.DependencyInjection;

namespace fluxel.Bot.Commands;

public class ModdingInactiveCommand : ISlashCommand
{
    public string Name => "modding-inactive";
    public string Description => "Marks a mapset as inactive and removes if from the modding queue.";

    public IEnumerable<ISlashCommand.Option> Options => new List<ISlashCommand.Option>
    {
        new(OptionType.Integer, "id", "The ID of the mapset to mark as inactive.", true)
    };

    public void Handle(DiscordInteraction interaction, IServiceProvider services)
    {
        var mm = services.GetRequiredService<MapManager>();
        var set = mm.GetSet(interaction.GetInt("id")!.Value);

        if (set is null)
        {
            interaction.Reply(ResponseStrings.MapSetNotFound, true);
            return;
        }

        if (set.Status != MapStatus.Pending)
        {
            interaction.Reply("Not in queue?", true);
            return;
        }

        if (!set.AddModdingEntry(APIModdingActionType.Deny, 0, mm, services.GetRequiredService<ScoreManager>(), out var error))
        {
            interaction.Reply(error, true);
            return;
        }

        var act = mm.CreateModAction(set.ID, 0, APIModdingActionType.Deny, "This mapset has been marked as inactive as it has not been updated in 2 weeks.");
        services.GetRequiredService<TaskRunner>().Schedule(new MethodTask(() => services.GetRequiredService<ServerEvents>().QueueActionCreate(act.ID)));
        interaction.Reply("okayge :+1::+1:", true);
    }
}
