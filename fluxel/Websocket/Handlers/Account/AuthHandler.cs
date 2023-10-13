using fluxel.API.Utils;
using fluxel.Components.Users;
using fluxel.Database.Helpers;
using Newtonsoft.Json.Linq;

namespace fluxel.Websocket.Handlers.Account;

public class AuthHandler : IPacketHandler {
    public void Handle(WebsocketInteraction interaction, JToken data) {
        var username = data["username"]?.Value<string>();
        var password = data["password"]?.Value<string>();

        if (username == null || password == null) {
            interaction.Reply(400, "Missing username or password!");
            return;
        }

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)) {
            interaction.Reply(400, "Username or password cannot be empty!");
            return;
        }

        var user = UserHelper.Get(username);

        if (user == null) {
            interaction.Reply(400, "There is no user with that username!");
            return;
        }

        if (!PasswordUtils.VerifyPassword(password, user.Password)) {
            interaction.Reply(400, "The provided password is incorrect!");
            return;
        }

        var token = UserToken.GetByUserId(user.Id);

        interaction.Reply(200, "Successfully logged in!", token.Token);
    }
}
