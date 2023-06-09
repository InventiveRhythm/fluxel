using fluxel.Components.Users;
using fluxel.Database;
using fluxel.Utils;
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
        
        if (string.IsNullOrEmpty(user.CountryCode)) {
            IpUtils.GetCountryCode(interaction.RemoteEndPoint.Address.ToString()).ContinueWith(task => {
                Console.WriteLine($"Got country code for {interaction.RemoteEndPoint.Address}: {task.Result}");
                RealmAccess.Run(realm => {
                    var u = realm.Find<User>(user.Id);
                    u.CountryCode = task.Result;
                });
            });
        }

        interaction.Reply(200, "Successfully logged in!", user.ToShort());
        Stats.AddOnlineUser(interaction.RemoteEndPoint, user.Id);
    }
}