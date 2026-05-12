using System;
using System.Collections.Generic;
using DSharpPlus;
using DSharpPlus.Entities;
using JetBrains.Annotations;

namespace fluxel.Bot.Components;

[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public interface ISlashCommand
{
    string Name { get; }
    string Description { get; }
    Permissions Permissions => Permissions.Administrator;
    IEnumerable<Option> Options => Array.Empty<Option>();

    void Handle(DiscordInteraction interaction, IServiceProvider services);

    public class Option
    {
        public string Name { get; }
        public string Description { get; }
        public OptionType Type { get; }
        public bool Required { get; }

        public List<Choice> Choices { get; } = new();

        public Option(OptionType type, string name, string desc, bool required)
        {
            Name = name;
            Description = desc;
            Type = type;
            Required = required;
        }

        public Option WithChoices(params Choice[] choices)
        {
            foreach (var choice in choices)
                WithChoice(choice.Name, choice.Value);

            return this;
        }

        public Option WithChoice(string name, object value)
        {
            if (value is not string or long or int or double)
                throw new ArgumentException("Value must be a string, long, int, or double.");

            Choices.Add(new Choice(name, value));
            return this;
        }
    }

    public class Choice
    {
        public string Name { get; }
        public object Value { get; }

        public Choice(string name, object value)
        {
            Name = name;
            Value = value;
        }
    }
}
