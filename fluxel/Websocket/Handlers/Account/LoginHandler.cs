using fluxel.Components.Users;
using fluxel.Constants;
using fluxel.Database.Helpers;
using Newtonsoft.Json.Linq;

namespace fluxel.Websocket.Handlers.Account;

public class LoginHandler : IPacketHandler {
    public async void Handle(WebsocketInteraction interaction, JToken data) {
        var token = data["token"]?.Value<string>();

        if (token == null) {
            interaction.Reply(400, ResponseStrings.NoToken);
            return;
        }

        var utk = UserToken.GetByToken(token);

        if (utk == null) {
            interaction.Reply(400, ResponseStrings.InvalidToken);
            return;
        }

        var user = UserHelper.Get(utk.Id);

        if (user == null) {
            interaction.Reply(400, ResponseStrings.TokenUserNotFound);
            return;
        }

        interaction.Reply(200, "Successfully logged in!", user.ToShort());
        GlobalStatistics.AddOnlineUser(interaction.RemoteEndPoint, user.Id);

        /*if (!string.IsNullOrEmpty(user.CountryCode)) return;

        var userid = user.Id;
        var code = await IpUtils.GetCountryCode(interaction.RemoteEndPoint.Address.ToString());
        RealmAccess.Run(realm => {
            var u = realm.Find<User>(userid);
            u.CountryCode = code;
        });*/
    }
}
