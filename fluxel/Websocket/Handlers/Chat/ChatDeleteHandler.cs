using fluxel.Components.Chat;
using fluxel.Components.Users;
using fluxel.Database;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Realms;

namespace fluxel.Websocket.Handlers.Chat; 

public class ChatDeleteHandler : IPacketHandler {
    public void Handle(WebsocketInteraction interaction, JToken data) {
        var id = data["id"]?.Value<string>();

        if (string.IsNullOrWhiteSpace(id)) {
            interaction.Reply(400, "Invalid message ID!");
            return;
        }
        
        if (!Stats.OnlineUsers.TryGetValue(interaction.RemoteEndPoint, out var userId)) {
            interaction.Reply(400, "You are not logged in!");
            return;
        }
        
        var user = User.FindById(userId);
        if (user == null) throw new Exception("User is null!");
        
        if (user.Role < 3) {
            interaction.Reply(400, "You are not allowed to delete messages!");
            return;
        }
        
        var guid = Guid.Parse(id);

        var success = RealmAccess.Run(realm => {
            var message = realm.Find<ChatMessage>(guid);
            if (message == null) return false;
            
            realm.Remove(message);
            return true;
        });
        
        if (!success) {
            interaction.Reply(400, "Message not found!");
            return;
        }
        
        foreach (var (_, conn) in WebsocketConnection.Connections) {
            var json = JsonConvert.SerializeObject(new {
                id = 12,
                status = 200,
                data = id
            });
            
            conn.Send(json);
        }
    }
}