using fluxel.Multiplayer.OpenLobby;
using Newtonsoft.Json.Linq;

namespace fluxel.Websocket.Handlers.Multiplayer;

public class MultiplayerLeaveHandler : IPacketHandler {
    public void Handle(WebsocketInteraction interaction, JToken data) {
        LobbyHandler.RemoveUser(interaction.RemoteEndPoint);
    }
}
