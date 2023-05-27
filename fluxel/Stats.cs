using System.Net;

namespace fluxel; 

public static class Stats {
    private static readonly Dictionary<IPEndPoint, int> OnlineUsers = new();
    
    public static int Online => OnlineUsers.Count;
    
    public static void AddOnlineUser(IPEndPoint ip, int id) {
        OnlineUsers[ip] = id;
    }
    
    public static void RemoveOnlineUser(IPEndPoint ip) {
        if (OnlineUsers.ContainsKey(ip))
            OnlineUsers.Remove(ip);
    }
}