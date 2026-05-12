using fluxel.Config;
using fluxel.Models.Maps;
using fluxel.Models.Users;

namespace fluxel.Components;

public class UrlFormatter
{
    private readonly ServerConfig config;

    public UrlFormatter(ServerConfig config)
    {
        this.config = config;
    }

    public string Web(User user) => $"{config.Urls.Website}/u/{user.ID}";
    public string Avatar(User user) => $"{config.Urls.Assets}/avatar/{user.AvatarHash}";
    public string Banner(User user) => $"{config.Urls.Assets}/banner/{user.BannerHash}";

    public string Web(MapSet set) => $"{config.Urls.Website}/set/{set.ID}";
    public string Background(MapSet set, bool lg = true) => $"{config.Urls.Assets}/background/{set.ID}{(lg ? "-lg" : "")}";
    public string Cover(MapSet set, bool lg = true) => $"{config.Urls.Assets}/cover/{set.ID}{(lg ? "-lg" : "")}";

    public string Web(Map map) => $"{config.Urls.Website}/set/{map.SetID}#{map.ID}";
    public string Background(Map map, bool lg = true) => $"{config.Urls.Assets}/background/{map.SetID}{(lg ? "-lg" : "")}";
    public string Cover(Map map, bool lg = true) => $"{config.Urls.Assets}/cover/{map.SetID}{(lg ? "-lg" : "")}";
}
