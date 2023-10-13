using DSharpPlus.Entities;
using fluxel.API.Utils;
using fluxel.Bot;
using fluxel.Components.Users;
using fluxel.Database.Helpers;
using fluxel.Utils;
using Newtonsoft.Json.Linq;

namespace fluxel.Websocket.Handlers.Account;

public class RegisterHandler : IPacketHandler {
    public async void Handle(WebsocketInteraction interaction, JToken data) {
        var username = data["username"]?.Value<string>();
        var email = data["email"]?.Value<string>();
        var password = data["password"]?.Value<string>();

        if (username == null || password == null || email == null) {
            interaction.Reply(400, "Missing username, email or password!");
            return;
        }

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)) {
            interaction.Reply(400, "Username or password cannot be empty!");
            return;
        }

        if (username.Length is < 3 or > 16) {
            interaction.Reply(400, "Username must be between 3 and 16 characters!");
            return;
        }

        if (password.Length is < 8 or > 32) {
            interaction.Reply(400, "Password must be between 8 and 32 characters!");
            return;
        }

        if (!username.ValidUsername()) {
            interaction.Reply(400, "Username can only contain A-Z, a-z, 0-9 and _!");
            return;
        }

        if (!MailUtils.IsValidEmail(email)) {
            interaction.Reply(400, "The provided email is invalid!");
            return;
        }

        if (username.UsernameExists()) {
            interaction.Reply(400, "Username is already taken!");
            return;
        }

        var user = new User {
            Id = UserHelper.NextId,
            Username = username,
            Email = email,
            Password = PasswordUtils.HashPassword(password),
            CountryCode = await IpUtils.GetCountryCode(interaction.RemoteEndPoint.Address.ToString())
        };

        UserHelper.Add(user);

        interaction.Reply(200, "Successfully registered!", new {
            token = UserToken.GetByUserId(user.Id).Token,
            user
        });

        GlobalStatistics.AddOnlineUser(interaction.RemoteEndPoint, user.Id);

        DiscordBot.GetLoggingChannel()?.SendMessageAsync(new DiscordMessageBuilder {
            Embed = new DiscordEmbedBuilder
            {
                Title = "New user registered!",
                Color = new DiscordColor("#55ff55")
            }.AddField("Username", user.Username, true).AddField("ID", $"{user.Id}", true).Build()
        });
    }
}
