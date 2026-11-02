using System;
using System.Collections.Generic;
using System.Text;
using DSharpPlus.Entities;
using fluxel.Bot.Components;
using fluxel.Bot.Utils;

namespace fluxel.Bot.Commands;

public class SendVerifyMessageCommand : ISlashCommand
{
    public string Name => "send-verify";
    public string Description => "sends the verification message in the current channel";
    public DiscordPermission Permissions => DiscordPermission.Administrator;
    public IEnumerable<ISlashCommand.Option> Options => [];

    public void Handle(DiscordInteraction interaction, IServiceProvider services)
    {
        var content = new StringBuilder();
        content.AppendLine("# Member Verification");
        content.AppendLine("In order to chat in this server you need to have your discord account linked to a fluXis account.");
        content.AppendLine("This should work automatically but in case it didn't you can press the button below to get the role.");

        var builder = new DiscordMessageBuilder { Content = content.ToString() };
        builder.AddActionRowComponent(new DiscordButtonComponent(DiscordButtonStyle.Primary, "user-verify", "Check Status"));

        _ = interaction.Channel.SendMessageAsync(builder).Result;
        interaction.Reply("okay", true);
    }
}
