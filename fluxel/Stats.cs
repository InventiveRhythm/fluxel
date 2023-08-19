using System.Net;
using fluxel.Components.Users;
using fluxel.Database;
using fluxel.Multiplayer.OpenLobby;

namespace fluxel;

public static class Stats {
    public static readonly Dictionary<IPEndPoint, int> ONLINE_USERS = new();

    public static int Online => ONLINE_USERS.Count;
    public static List<int> GetOnlineUsers => ONLINE_USERS.Values.ToList();

    public static void AddOnlineUser(IPEndPoint ip, int id) {
        if (ONLINE_USERS.ContainsValue(id))
            ONLINE_USERS.Remove(ONLINE_USERS.First(x => x.Value == id).Key);

        ONLINE_USERS[ip] = id;
    }

    public static void RemoveOnlineUser(IPEndPoint ip) {
        if (ONLINE_USERS.ContainsKey(ip)) {
            RealmAccess.Run(_ => {
                var user = User.FindById(ONLINE_USERS[ip]);
                if (user != null)
                    user.LastLogin = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            });

            ONLINE_USERS.Remove(ip);
        }

        LobbyHandler.RemoveUser(ip);
    }
}
