using fluxel.Components.Users;
using Newtonsoft.Json.Linq;

namespace fluxel.Websocket.Handlers.Account; 

public class LoginHandler : IPacketHandler {
    public void Handle(WebsocketInteraction interaction, JToken data) {
        var token = data["token"]?.Value<string>();
        
        if (token == null) {
            interaction.Reply(400, "Missing token!");
            return;
        }
        
        var utk = UserToken.GetByToken(token);
        
        if (utk == null) {
            interaction.Reply(400, "Invalid token!");
            return;
        }
        
        var user = User.FindById(utk.UserId);
        
        if (user == null) {
            interaction.Reply(400, "Invalid token! (User not found)");
            return;
        }
        
        interaction.Reply(200, "Successfully logged in!", user.ToShort());
    }
}