using fluxel.Database.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace fluxel.Websocket.Handlers.Chat;

public class ChatDeleteHandler : IPacketHandler {
    public void Handle(WebsocketInteraction interaction, JToken data) {
        var id = data["id"]?.Value<string>();

        if (string.IsNullOrWhiteSpace(id)) {
            interaction.Reply(400, "Invalid message ID!");
            return;
        }

        if (!GlobalStatistics.ONLINE_USERS.TryGetValue(interaction.RemoteEndPoint, out var userId)) {
            interaction.Reply(400, "You are not logged in!");
            return;
        }

        var user = UserHelper.Get(userId);
        if (user == null) throw new Exception("User is null!");

        if (user.Role < 3) {
            interaction.Reply(400, "You are not allowed to delete messages!");
            return;
        }

        var guid = Guid.Parse(id);

        var success = false;

        var message = ChatHelper.Get(guid);

        if (message != null) {
            ChatHelper.Delete(message);
            success = true;
        }

        if (!success) {
            interaction.Reply(400, "Message not found!");
            return;
        }

        foreach (var (_, conn) in WebsocketConnection.CONNECTIONS) {
            var json = JsonConvert.SerializeObject(new {
                id = 12,
                status = 200,
                data = id
            });

            conn.Send(json);
        }
    }
}
