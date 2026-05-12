using System;
using System.Collections.Generic;
using System.Linq;
using DSharpPlus;
using DSharpPlus.Entities;
using fluxel.Bot.Components;
using fluxel.Bot.Utils;
using fluxel.Components;
using fluxel.Database;
using fluxel.Modules;
using fluxel.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace fluxel.Bot.Commands;

public class UserCommand : ISlashCommand
{
    public string Name => "user";
    public string Description => "Get information about a user.";
    public Permissions Permissions => Permissions.SendMessages;

    public IEnumerable<ISlashCommand.Option> Options => new List<ISlashCommand.Option>
    {
        new(OptionType.String, "user", "The user to get information about.", true)
    };

    public void Handle(DiscordInteraction interaction, IServiceProvider services)
    {
        var users = services.GetRequiredService<UserManager>();
        var groups = services.GetRequiredService<GroupManager>();
        var urls = services.GetRequiredService<UrlFormatter>();
        var cache = services.GetRequiredService<RequestCache>();
        var clubs = services.GetRequiredService<ClubManager>();

        var userstr = interaction.GetString("user")!;

        var user = int.TryParse(userstr, out var id) ? users.Get(id) : users.Get(userstr);

        if (user == null)
        {
            interaction.Reply("User not found.", true);
            return;
        }

        var group = user.GetGroups(groups).FirstOrDefault();
        var color = group is null ? DiscordColor.Blurple : ColorUtils.ParseHex(group.Color);

        var embed = new DiscordEmbedBuilder
        {
            Author = user.ToEmbedAuthor(urls),
            Color = color,
            ImageUrl = urls.Banner(user),
            Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail { Url = urls.Avatar(user) }
        };

        if (!string.IsNullOrEmpty(user.DisplayName))
            embed.AddField("Display Name", user.DisplayName, true);

        var club = clubs.GetWhereUserIsMember(user.ID);

        if (club != null)
            embed.AddField("Club", club.Name, true);

        embed.AddField("Registered", $"<t:{user.CreatedAt}:R>", true);
        embed.AddField("Last Seen", services.GetService<IOnlineStateManager>()?.IsOnline(user.ID) ?? false ? "Right Now" : $"<t:{user.LastLogin}:R>", true);
        embed.AddField("Global Rank", $"#{user.GetGlobalRank(cache)}", true);
        embed.AddField("Country Rank", $"#{user.GetCountryRank(cache)}", true);
        embed.AddField("Overall Rating", $"{user.OverallRating:0.00}", true);
        embed.AddField("Potential Rating", $"{user.PotentialRating:0.00}", true);
        embed.WithFooter($"ID: {user.ID}");

        interaction.ReplyEmbed(embed);
    }
}
