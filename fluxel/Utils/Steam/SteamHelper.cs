using System.Net;
using System.Threading.Tasks;
using fluxel.Config;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;

namespace fluxel.Utils.Steam;

public static class SteamHelper
{
    public static async Task<ulong> FetchUserIDFromTicket(ServerConfig.SteamConfig config, string ticket)
    {
        var usr = new SteamWebInterfaceFactory(config.WebKey).CreateSteamWebInterface<SteamUserAuth>();
        var res = await usr.AuthenticateUserTicket(config.AppID, ticket);

        return res.Data.Response.Success
            ? ulong.Parse(res.Data.Response.Params.SteamId)
            : throw new WebException($"{res.Data.Response.Error.ErrorCode}: {res.Data.Response.Error.ErrorDesc}");
    }
}
