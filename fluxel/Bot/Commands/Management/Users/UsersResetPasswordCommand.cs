using System;
using System.Collections.Generic;
using DSharpPlus;
using DSharpPlus.Entities;
using fluxel.Bot.Components;
using fluxel.Bot.Utils;
using fluxel.Database;
using fluxel.Models.OAuth;
using Microsoft.Extensions.DependencyInjection;
using Midori.Logging;

namespace fluxel.Bot.Commands.Management.Users;

public class UsersResetPasswordCommand : ISlashCommand
{
    public string Name => "reset-password";
    public string Description => "Create a password reset token.";
    public Permissions Permissions => Permissions.Administrator;

    public IEnumerable<ISlashCommand.Option> Options => new List<ISlashCommand.Option>
    {
        new(OptionType.Integer, "user", "The user to create a password reset token for", true)
    };

    public void Handle(DiscordInteraction interaction, IServiceProvider services)
    {
        var users = services.GetRequiredService<UserManager>();

        try
        {
            var id = interaction.GetInt("user");
            if (id == null) return;

            var user = users.Get(id.Value);

            if (user == null)
            {
                interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder
                {
                    Content = "The provided user ID is invalid.",
                    IsEphemeral = true
                });
                return;
            }

            var token = services.GetRequiredService<OAuthManager>().CreateToken(user.ID, 4, OAuthScopes.PasswordReset);
            interaction.Reply($"Created password reset token for **{user.Username}**\n`https://auth.flux.moe/reset?token={token.AccessToken}`", true);
        }
        catch (Exception e)
        {
            Logger.Error(e, "Failed to create password reset token.");

            interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder
            {
                Content = "shitdt i fuckeded up",
                IsEphemeral = true
            });
        }
    }
}
