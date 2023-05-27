using System.Net;
using fluxel.Components.Users;
using fluxel.Database;

namespace fluxel; 

public static class Stats {
    private static readonly Dictionary<IPEndPoint, int> OnlineUsers = new();
    
    public static int Online => OnlineUsers.Count;
    public static List<int> GetOnlineUsers => OnlineUsers.Values.ToList();

    public static void AddOnlineUser(IPEndPoint ip, int id) {
        OnlineUsers[ip] = id;
    }
    
    public static void RemoveOnlineUser(IPEndPoint ip) {
        if (OnlineUsers.ContainsKey(ip)) {
            RealmAccess.Run(_ => {
                var user = User.FindById(OnlineUsers[ip]);
                if (user != null)
                    user.LastLogin = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            });
            
            OnlineUsers.Remove(ip);
        }
    }
}