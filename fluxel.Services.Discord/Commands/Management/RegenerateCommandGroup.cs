using System.Collections.Generic;
using fluxel.Services.Discord.Commands.Management.Regenerate;
using fluxel.Services.Discord.Components;

namespace fluxel.Services.Discord.Commands.Management;

public class RegenerateCommandGroup : ISlashCommandGroup
{
    public string Name => "regenerate";
    public string Description => "Regenerate different things.";

    public IEnumerable<ISlashCommand> Subcommands => new List<ISlashCommand>
    {
        new RegenerateMapSetAssetsCommand()
    };
}
