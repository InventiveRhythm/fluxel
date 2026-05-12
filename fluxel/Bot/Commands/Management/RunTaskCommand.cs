using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using DSharpPlus.Entities;
using fluxel.Bot.Components;
using fluxel.Bot.Utils;
using fluxel.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Midori.Logging;
using Midori.Utils.Extensions;

namespace fluxel.Bot.Commands.Management;

public class RunTaskCommand : ISlashCommand
{
    public string Name => "run-task";
    public string Description => "Run a task";

    public IEnumerable<ISlashCommand.Option> Options => new List<ISlashCommand.Option>
    {
        new(OptionType.String, "name", "The name of the task.", true),
        new(OptionType.String, "args", "Constructor arguments, seperated by a colon. (:)", true),
    };

    public async void Handle(DiscordInteraction interaction, IServiceProvider services)
    {
        await interaction.AcknowledgeEphemeral();

        var name = interaction.GetString("name")!;
        var strArgs = interaction.GetString("args") ?? "";
        var argSplit = strArgs.Split(":");

        var tasks = Assembly.GetExecutingAssembly()
                            .GetTypes().Where(t => t.GetInterfaces().Contains(typeof(IBasicTask)));

        var taskType = tasks.FirstOrDefault(t => t.Name.EqualsLower(name));

        if (taskType == null)
        {
            interaction.Followup("No task with this name found.", true);
            return;
        }

        var constructor = taskType.GetConstructors()[0];
        var parameters = new List<object>();

        Logger.Log($"got ctor");

        foreach (var param in constructor.GetParameters())
        {
            Logger.Log(param.ParameterType.Name);

            switch (param.ParameterType.Name)
            {
                case "Int64":
                    parameters.Add(long.Parse(argSplit[param.Position], CultureInfo.InvariantCulture));
                    break;
            }
        }

        try
        {
            var task = Activator.CreateInstance(taskType, parameters.ToArray());
            Logger.Log("created instance");
            services.GetRequiredService<TaskRunner>().Schedule((task as IBasicTask)!);
            Logger.Log("scheduled");
            interaction.Followup("done");
        }
        catch (Exception ex)
        {
            interaction.Followup(ex.Message, true);
        }
    }
}
