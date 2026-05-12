using System;
using System.Globalization;
using DSharpPlus.Entities;
using fluxel.Bot;
using fluxel.Database;
using fluxel.Models.Maps;
using fluxel.Models.Scores;
using fluxel.Models.Users;
using fluxel.Utils;
using fluXis.Graphics.UserInterface.Color;
using fluXis.Online.API.Models.Maps.Modding;
using MongoDB.Bson;

namespace fluxel.Components;

public class ServerEvents
{
    private readonly MapManager maps;
    private readonly UserManager users;
    private readonly ScoreManager scores;
    private readonly UrlFormatter urls;
    private readonly DiscordBot discord;

    public ServerEvents(MapManager maps, UserManager users, UrlFormatter urls, DiscordBot discord, ScoreManager scores)
    {
        this.maps = maps;
        this.users = users;
        this.urls = urls;
        this.discord = discord;
        this.scores = scores;
    }

    public void UploadMap(long mapsetId)
    {
        var set = maps.GetSet(mapsetId) ?? throw new ArgumentNullException(nameof(MapSet), "erm");
        var creator = users.Get(set.CreatorID) ?? throw new ArgumentNullException(nameof(User), "erm");

        var embed = new DiscordEmbedBuilder
            {
                Title = "New mapset uploaded!",
                Author = creator.ToEmbedAuthor(urls),
                Color = new DiscordColor("#55ff55"),
                Url = urls.Web(set)
            }.AddField("Title", string.IsNullOrEmpty(set.Title) ? "<empty>" : set.Title, true)
             .AddField("Artist", string.IsNullOrEmpty(set.Artist) ? "<empty>" : set.Artist, true)
             .WithThumbnail(urls.Cover(set))
             .WithImageUrl(urls.Background(set));

        discord.GetChannel(DiscordBot.ChannelType.MapSubmissions)?.SendMessageAsync(embed.Build());
    }

    public void MapPure(long mapsetId)
    {
        var set = maps.GetSet(mapsetId) ?? throw new ArgumentNullException(nameof(MapSet), "erm");
        var creator = users.Get(set.CreatorID) ?? throw new ArgumentNullException(nameof(User), "erm");

        var embed = new DiscordEmbedBuilder
            {
                Title = "New mapset purified!",
                Author = creator.ToEmbedAuthor(urls),
                Color = new DiscordColor("#55b2ff"),
                Url = urls.Web(set)
            }.AddField("Title", string.IsNullOrEmpty(set.Title) ? "<empty>" : set.Title, true)
             .AddField("Artist", string.IsNullOrEmpty(set.Artist) ? "<empty>" : set.Artist, true)
             .WithThumbnail(urls.Cover(set))
             .WithImageUrl(urls.Background(set));

        var message = new DiscordMessageBuilder()
            .WithEmbed(embed.Build());

        try
        {
            if (creator.DiscordID != null)
            {
                message.Content = $"<@{creator.DiscordID}>";
                message.WithAllowedMention(new UserMention(creator.DiscordID.Value));
            }
        }
        catch { }

        discord.GetChannel(DiscordBot.ChannelType.MapRanked)?.SendMessageAsync(message);
    }

    public void QueueActionCreate(ObjectId id)
    {
        var action = maps.GetAction(id);
        if (action is null) return;

        var set = maps.GetSet(action.MapSetID);
        if (set is null) return;

        var user = users.Get(action.UserID);
        if (user is null) return;

        var mapper = users.Get(set.CreatorID);
        if (mapper is null) return;

        var message = new DiscordMessageBuilder();
        var embed = new DiscordEmbedBuilder();

        var link = $"[{set.Artist[..Math.Min(set.Artist.Length, 256)]} - {set.Title[..Math.Min(set.Title.Length, 256)]}]({urls.Web(set)}/modding)";

        switch (action.Type)
        {
            case APIModdingActionType.Note:
                embed = embed.WithDescription($"<:QueueNote:1387265258917199993> Added a note to {link}").WithColor(Theme.Blue.ToDiscord());
                break;

            case APIModdingActionType.Approve:
                embed = embed.WithDescription($"<:QueueApprove:1387265241666293780> Approved {link}").WithColor(Theme.Green.ToDiscord());
                break;

            case APIModdingActionType.Deny:
                embed = embed.WithDescription($"<:QueueDeny:1387265252248518769> Denied {link}").WithColor(Theme.Red.ToDiscord());
                break;

            case APIModdingActionType.Submitted:
                embed = embed.WithDescription($"<:QueueSubmit:1387265266697769073> Submitted {link} to the queue").WithColor(new DiscordColor("#FF55C6"));
                break;

            case APIModdingActionType.Update:
                embed = embed.WithDescription($"<:QueueUpdate:1387265275099086848> Updated {link}").WithColor(Theme.Yellow.ToDiscord());
                break;
        }

        embed.Author = user.ToEmbedAuthor(urls);
        embed = embed.WithThumbnail(urls.Cover(set));

        if (user.ID != mapper.ID && mapper.DiscordID != null)
            message = message.WithAllowedMention(new UserMention(mapper.DiscordID.Value)).WithContent($"<@{mapper.DiscordID}>");

        message = message.WithEmbed(embed);
        discord.GetChannel(DiscordBot.ChannelType.Queue)?.SendMessageAsync(message);
    }

    public void FirstPlace(Map map, Score? currentFirst)
    {
        var newFirst = scores.GetFirst(map.ID);

        // did not change
        if (newFirst != null && currentFirst?.ID == newFirst.ID)
            return;

        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        var user = users.Get(newFirst!.UserID)!;

        var embed = new DiscordEmbedBuilder
            {
                Color = new DiscordColor("#55ff55"),
                Author = user.ToEmbedAuthor(urls),
                Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail { Url = urls.Cover(map) },
                ImageUrl = urls.Background(map)
            }.AddField("Map", $"[{map.Metadata}]({urls.Web(map)})")
             .AddField("PR", $"{newFirst.PerformanceRating:00.00}pr", true)
             .AddField("Accuracy", $"{newFirst.Accuracy:00.00}%", true)
             .AddField("Max Combo", $"{newFirst.MaxCombo}x", true);

        if (!string.IsNullOrEmpty(newFirst.Mods))
            embed.AddField("Mods", $"{newFirst.Mods}", true);

        if (currentFirst != null)
        {
            var cfUser = users.Get(currentFirst.UserID);
            embed.AddField("Previous First Place", $"{cfUser?.Username ?? "(could not get user)"}", true);
            embed.AddField("Previous PR", $"{currentFirst.PerformanceRating:00.00}pr", true);
        }

        discord.GetChannel(DiscordBot.ChannelType.MapFirstPlace)?.SendMessageAsync(embed.Build());
    }

    public void NotifySameEmail(User existing)
    {
        discord.GetChannel(DiscordBot.ChannelType.Registrations)?.SendMessageAsync(new DiscordMessageBuilder
        {
            Embed = new DiscordEmbedBuilder
            {
                Author = existing.ToEmbedAuthor(urls),
                Description = "Someone tried to register with an existing email!",
                Color = new DiscordColor("#ff5555")
            }.WithFooter($"ID: {existing.ID}").Build()
        });
    }
}
