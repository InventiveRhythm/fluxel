using System;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using fluxel.Bot;
using fluxel.Components;
using fluxel.Database;
using fluxel.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace fluxel.Tasks.Logging;

public class LogUserRegistrationTask : IBasicTask
{
    public string Name => $"LogUserRegistration({id})";

    private long id { get; }

    public LogUserRegistrationTask(long id)
    {
        this.id = id;
    }

    public Task Run(IServiceProvider services)
    {
        var user = services.GetRequiredService<UserManager>().Get(id) ?? throw new ArgumentException($"No user with id {id} was found!");

        services.GetRequiredService<DiscordBot>().GetChannel(DiscordBot.ChannelType.Registrations)?.SendMessageAsync(new DiscordMessageBuilder
        {
            Embed = new DiscordEmbedBuilder
            {
                Author = user.ToEmbedAuthor(services.GetRequiredService<UrlFormatter>()),
                Description = "Just registered!",
                Color = new DiscordColor("#55ff55")
            }.WithFooter($"ID: {user.ID}").Build()
        });

        return Task.CompletedTask;
    }
}
