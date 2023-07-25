using fluxel.Database;
using Realms;

namespace fluxel.Components.Users;

public class UserToken : RealmObject {
    [Indexed]
    public int UserId { get; init; }

    [Required]
    [PrimaryKey]
    public string Token { get; set; } = string.Empty;

    public static string GenerateToken() {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 32).Select(s => s[random.Next(s.Length)]).ToArray());
    }

    public static UserToken? GetByToken(string token) => RealmAccess.Run(realm => realm.Find<UserToken>(token));

    public static UserToken GetByUserId(int id) {
        return RealmAccess.Run(realm => {
            var tk = realm.All<UserToken>().FirstOrDefault(t => t.UserId == id);

            if (tk == null) {
                realm.Add(tk = new UserToken {
                    UserId = id,
                    Token = GenerateToken()
                });
            }

            return tk;
        });
    }
}
