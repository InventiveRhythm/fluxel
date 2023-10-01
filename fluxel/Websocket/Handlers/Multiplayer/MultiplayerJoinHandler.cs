using fluxel.Multiplayer.OpenLobby;
using Newtonsoft.Json.Linq;

namespace fluxel.Websocket.Handlers.Multiplayer;

public class MultiplayerJoinHandler : IPacketHandler {
    public void Handle(WebsocketInteraction interaction, JToken data) {
        var lobbyId = data["lobbyId"]?.ToObject<int>() ?? -1;
        var password = data["password"]?.ToObject<string>() ?? "";

        var user = GlobalStatistics.ONLINE_USERS.FirstOrDefault(u => Equals(u.Key, interaction.RemoteEndPoint)).Value;

        if (user == 0) {
            interaction.Reply(400, "User not found.");
            return;
        }

        var lobby = LobbyHandler.Lobbies.Find(l => l.RoomId == lobbyId);

        if (lobby == null) {
            interaction.Reply(400, "Lobby not found.");
            return;
        }

        if (lobby.Settings.HasPassword && lobby.Settings.Password != password) {
            interaction.Reply(400, "Invalid password.");
            return;
        }

        if (!LobbyHandler.AddUser(lobbyId, interaction.RemoteEndPoint, user)) {
            interaction.Reply(400, "Failed to add user to lobby.");
            return;
        }

        interaction.Reply(200, "Successfully joined lobby.", lobby);
    }
}
