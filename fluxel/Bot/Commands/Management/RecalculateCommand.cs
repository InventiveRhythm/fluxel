using System;
using System.Collections.Generic;
using DSharpPlus.Entities;
using fluxel.Bot.Components;
using fluxel.Bot.Utils;
using fluxel.Database;
using fluxel.Tasks;
using fluxel.Tasks.Clubs;
using fluxel.Tasks.Maps;
using fluxel.Tasks.MapSets;
using fluxel.Tasks.Scores;
using fluxel.Tasks.Users;
using Microsoft.Extensions.DependencyInjection;

namespace fluxel.Bot.Commands.Management;

public class RecalculateCommand : ISlashCommand
{
    public string Name => "recalculate";
    public string Description => "Recalculate different things.";

    public IEnumerable<ISlashCommand.Option> Options => new List<ISlashCommand.Option>
    {
        new ISlashCommand.Option(OptionType.String, "type", "The type of recalculation to perform.", true).WithChoices(
            new ISlashCommand.Choice("Clubs", "clubs"),
            new ISlashCommand.Choice("Club Claims", "club-claims"),
            new ISlashCommand.Choice("Club Scores", "club-scores"),
            new ISlashCommand.Choice("Maps", "maps"),
            new ISlashCommand.Choice("Map filenames", "map-filenames"),
            new ISlashCommand.Choice("Previews", "previews"),
            new ISlashCommand.Choice("Scores", "scores"),
            new ISlashCommand.Choice("Users", "users")
        )
    };

    public async void Handle(DiscordInteraction interaction, IServiceProvider services)
    {
        await interaction.AcknowledgeEphemeral();

        var type = interaction.GetString("type");
        var tasks = services.GetRequiredService<TaskRunner>();

        switch (type)
        {
            case "clubs":
                services.GetRequiredService<ClubManager>().All.ForEach(c => tasks.Schedule(new RecalculateClubTask(c.ID)));
                break;

            case "club-claims":
                tasks.Schedule(new RefreshClubClaimsBulkTask());
                break;

            case "club-scores":
            {
                var maps = services.GetRequiredService<MapManager>().AllMaps;
                services.GetRequiredService<ClubManager>().All.ForEach(c => maps.ForEach(m => tasks.Schedule(new RecalculateClubScoreTask(m.ID, c.ID))));
                break;
            }

            case "maps":
                services.GetRequiredService<MapManager>().AllMaps.ForEach(m => tasks.Schedule(new RecalculateMapTask(m.ID)));
                break;

            case "previews":
                tasks.Schedule(new RegeneratePreviewsBulkTask());
                break;

            case "scores":
                services.GetRequiredService<ScoreManager>().All.ForEach(s => tasks.Schedule(new RecalculateScoreTask(s.ID)));
                break;

            case "users":
                services.GetRequiredService<UserManager>().AllUsers.ForEach(u => tasks.Schedule(new RecalculateUserTask(u.ID)));
                break;

            default:
                interaction.Reply("Invalid recalculation type.", true);
                break;
        }

        interaction.Followup("Created tasks!");
    }
}
