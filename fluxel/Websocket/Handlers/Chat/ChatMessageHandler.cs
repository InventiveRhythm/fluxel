using fluxel.Components.Chat;
using fluxel.Database.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace fluxel.Websocket.Handlers.Chat;

public class ChatMessageHandler : IPacketHandler {
    public void Handle(WebsocketInteraction interaction, JToken data) {
        var content = data["content"]?.Value<string>();
        var channel = data["channel"]?.Value<string>();

        if (string.IsNullOrWhiteSpace(content) || channel == null)
            return;

        if (!GlobalStatistics.ONLINE_USERS.TryGetValue(interaction.RemoteEndPoint, out var id))
            return;

        var user = UserHelper.Get(id);
        if (user == null)
            return;

        var message = new ChatMessage {
            SenderId = user.Id,
            Content = content,
            Channel = channel
        };

        ChatHelper.Add(message);

        foreach (var (_, conn) in WebsocketConnection.CONNECTIONS) {
            var json = JsonConvert.SerializeObject(new {
                id = 10,
                data = message
            });

            conn.Send(json);
        }
    }
}
