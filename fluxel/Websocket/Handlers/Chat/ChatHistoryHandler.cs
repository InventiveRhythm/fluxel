using fluxel.Components.Chat;
using fluxel.Database;
using Newtonsoft.Json.Linq;

namespace fluxel.Websocket.Handlers.Chat; 

public class ChatHistoryHandler : IPacketHandler {
    public void Handle(WebsocketInteraction interaction, JToken data) {
        var channel = data["channel"]?.Value<string>();
        
        if (channel == null) {
            interaction.Reply(400, "Invalid channel!");
            return;
        }
        
        var messages = RealmAccess.Run(realm => realm.All<ChatMessage>()
            .Where(x => x.Channel == channel)
            .OrderByDescending(x => x.CreatedAt)
            .ToList()
            .Take(50));
        
        interaction.Reply(200, "OK!", messages);
    }
}