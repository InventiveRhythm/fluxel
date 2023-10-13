using System.Net;
using fluxel.Database.Helpers;
using fluxel.Multiplayer.OpenLobby;

namespace fluxel;

public static class GlobalStatistics {
    public static readonly Dictionary<IPEndPoint, long> ONLINE_USERS = new();

    public static int Online => ONLINE_USERS.Count - 1; // -1 because fluxel is always online
    public static List<long> GetOnlineUsers => ONLINE_USERS.Values.ToList();

    public static void AddOnlineUser(IPEndPoint ip, long id) {
        if (ONLINE_USERS.ContainsValue(id))
            ONLINE_USERS.Remove(ONLINE_USERS.First(x => x.Value == id).Key);

        ONLINE_USERS[ip] = id;
    }

    public static void RemoveOnlineUser(IPEndPoint ip) {
        if (ONLINE_USERS.ContainsKey(ip)) {
            var user = UserHelper.Get(ONLINE_USERS[ip]);

            if (user != null)
            {
                user.LastLogin = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                UserHelper.Update(user);
            }

            ONLINE_USERS.Remove(ip);
        }

        LobbyHandler.RemoveUser(ip);
    }
}
