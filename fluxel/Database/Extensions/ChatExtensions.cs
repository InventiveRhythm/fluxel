using fluxel.Database.Helpers;
using fluxel.Models.Chat;
using fluXis.Online.API.Models.Chat;

namespace fluxel.Database.Extensions;

public static class ChatExtensions
{
    public static APIChatChannel ToAPI(this ChatChannel channel) => new()
    {
        Name = channel.Name,
        Type = channel.Type,
        UserCount = channel.Users.Count,
        Target1 = channel.Target1 is null ? null : UserHelper.Get(channel.Target1.Value)?.ToAPI(),
        Target2 = channel.Target2 is null ? null : UserHelper.Get(channel.Target2.Value)?.ToAPI(),
        Club = channel.Club is null ? null : ClubHelper.Get(channel.Club.Value)?.ToAPI()
    };
}
