using System;
using System.Collections.Generic;
using DSharpPlus.Entities;
using fluxel.Bot.Components;
using fluxel.Bot.Utils;
using fluxel.Components;
using Microsoft.Extensions.DependencyInjection;
using Midori.Logging;

namespace fluxel.Bot.Commands.Management;

public class SendKoFiEmail : ISlashCommand
{
    public string Name => "send-kofi";
    public string Description => "Send a ko-fi link email.";

    public IEnumerable<ISlashCommand.Option> Options => new List<ISlashCommand.Option>
    {
        new(OptionType.String, "email", "the target", true),
    };

    public void Handle(DiscordInteraction interaction, IServiceProvider services)
    {
        try
        {
            var mail = interaction.GetString("email") ?? throw new ArgumentNullException();
            services.GetRequiredService<Donations>().SendLink(mail);
            interaction.Reply("cool", true);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "uhuhughuguh");
            interaction.ReplyEmbed(new DiscordEmbedBuilder
            {
                Title = "fuck.",
                Description = "check server logs...",
                Color = new DiscordColor(0xff5555)
            }, true);
        }
    }
}
