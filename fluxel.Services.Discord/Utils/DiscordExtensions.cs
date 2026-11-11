using DSharpPlus.Entities;
using fluxel.Components;
using fluxel.Models.Users;

namespace fluxel.Services.Discord.Utils;

public static class DiscordExtensions
{
    public static DiscordEmbedBuilder.EmbedAuthor ToEmbedAuthor(this User user, UrlFormatter urls) => new()
    {
        Name = user.Username,
        IconUrl = urls.Avatar(user.Profile ?? new UserProfile()),
        Url = urls.Web(user)
    };
}
