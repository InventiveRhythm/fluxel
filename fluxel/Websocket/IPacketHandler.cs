using Newtonsoft.Json.Linq;

namespace fluxel.Websocket;

public interface IPacketHandler {
    public void Handle(WebsocketInteraction interaction, JToken data);
}
