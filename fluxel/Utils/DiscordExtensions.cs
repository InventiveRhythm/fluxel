using DSharpPlus.Entities;
using fluxel.Components;
using fluxel.Models.Users;

namespace fluxel.Utils;

public static class DiscordExtensions
{
    public static DiscordEmbedBuilder.EmbedAuthor ToEmbedAuthor(this User user, UrlFormatter urls) => new()
    {
        Name = user.Username,
        IconUrl = urls.Avatar(user),
        Url = urls.Web(user)
    };
}
