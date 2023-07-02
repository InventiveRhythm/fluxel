using fluxel.Components.Chat;
using fluxel.Components.Users;
using fluxel.Database;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace fluxel.Websocket.Handlers.Chat; 

public class ChatMessageHandler : IPacketHandler {
    public void Handle(WebsocketInteraction interaction, JToken data) {
        var content = data["content"]?.Value<string>();
        var channel = data["channel"]?.Value<string>();

        if (string.IsNullOrWhiteSpace(content) || channel == null)
            return;
        
        if (!Stats.OnlineUsers.TryGetValue(interaction.RemoteEndPoint, out var id))
            return;
        
        var user = User.FindById(id);
        if (user == null)
            return;

        var message = new ChatMessage {
            SenderId = user.Id,
            Content = content,
            Channel = channel
        };

        RealmAccess.Run(realm => realm.Add(message));
        
        foreach (var (_, conn) in WebsocketConnection.Connections) {
            var json = JsonConvert.SerializeObject(new {
                id = 10,
                data = message
            });
            
            conn.Send(json);
        }
    }
}