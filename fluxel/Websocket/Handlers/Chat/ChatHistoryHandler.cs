using fluxel.Database.Helpers;
using Newtonsoft.Json.Linq;

namespace fluxel.Websocket.Handlers.Chat;

public class ChatHistoryHandler : IPacketHandler {
    public void Handle(WebsocketInteraction interaction, JToken data) {
        var channel = data["channel"]?.Value<string>();

        if (channel == null) {
            interaction.Reply(400, "Invalid channel!");
            return;
        }

        var messages = ChatHelper.FromChannel(channel)
            .OrderByDescending(x => x.CreatedAt)
            .ToList()
            .Take(50);

        interaction.Reply(200, "OK!", messages);
    }
}
