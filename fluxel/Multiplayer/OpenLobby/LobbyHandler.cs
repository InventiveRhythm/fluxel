using System.Net;
using fluxel.Components.Users;

namespace fluxel.Multiplayer.OpenLobby; 

public static class LobbyHandler {
    public static List<MultiLobby> Lobbies { get; set; } = new();
    
    public static void AddLobby(MultiLobby lobby) {
        Lobbies.Add(lobby);
    }
    
    public static bool AddUser(int lobbyId, IPEndPoint ip, int userId) {
        var lobby = Lobbies.Find(l => l.RoomId == lobbyId);
        if (lobby == null) return false;
        
        lobby.Users.Add(ip, userId);
        
        lobby.SendToAll(new {
            id = 23,
            data = new {
                type = "player/join",
                data = User.FindById(userId)?.ToShort() ?? new UserShort()
            }
        });
        
        return true;
    }
    
    public static bool RemoveUser(int lobbyId, int userId) {
        var lobby = Lobbies.Find(l => l.RoomId == lobbyId);
        if (lobby == null) return false;
        
        var entry = lobby.Users.FirstOrDefault(u => u.Value == userId);
        if (entry.Value == 0) return false;

        lobby.Users.Remove(entry.Key);
        
        lobby.SendToAll(new {
            id = 23,
            data = new {
                type = "player/leave",
                data = userId
            }
        });
        
        return true;
    }
    
    public static void RemoveUser(IPEndPoint ip) {
        var lobbies = Lobbies.Where(l => l.Users.ContainsKey(ip)).ToList();
        if (!lobbies.Any()) return;
        
        foreach (var lobby in lobbies) {
            lobby.Users.Remove(ip, out var userId);
            lobby.SendToAll(new {
                id = 23,
                data = new {
                    type = "player/leave",
                    data = userId
                }
            });
        }
    }
}