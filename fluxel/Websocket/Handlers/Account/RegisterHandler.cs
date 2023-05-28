using System.Text.RegularExpressions;
using fluxel.API.Components;
using fluxel.API.Utils;
using fluxel.Components.Users;
using fluxel.Database;
using Newtonsoft.Json.Linq;

namespace fluxel.Websocket.Handlers.Account; 

public class RegisterHandler : IPacketHandler {
    public void Handle(WebsocketInteraction interaction, JToken data) {
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
        
        // regex matching email
        if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")) {
            interaction.Reply(400, "Invalid email!");
            return;
        }
        
        if (User.UsernameExists(username)) {
            interaction.Reply(400, "Username is already taken!");
            return;
        }
        
        var user = new User {
            Id = User.GetNextId(),
            Username = username,
            Email = email,
            Password = PasswordUtils.HashPassword(password)
        };
        
        interaction.Reply(200, "Successfully registered!", new {
            token = UserToken.GetByUserId(user.Id).Token,
            user = RealmAccess.Run(realm => realm.Add(user))
        });
        
        Stats.AddOnlineUser(interaction.RemoteEndPoint, user.Id);
    }
}