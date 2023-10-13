using System.ComponentModel.DataAnnotations;
using fluxel.Database;
using MongoDB.Driver;

namespace fluxel.Components.Users;

public class UserToken {
    public long Id { get; init; }

    [Required]
    public string Token { get; set; } = string.Empty;

    public static string GenerateToken() {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 32).Select(s => s[random.Next(s.Length)]).ToArray());
    }

    public static UserToken? GetByToken(string token)
    {
        var tokens = MongoDatabase.GetCollection<UserToken>("tokens");
        return tokens.Find(t => t.Token == token).FirstOrDefault();
    }

    public static UserToken GetByUserId(long id) {
        var tokens = MongoDatabase.GetCollection<UserToken>("tokens");
        var token = tokens.Find(t => t.Id == id).FirstOrDefault();

        if (token == null) {
            token = new UserToken {
                Id = id,
                Token = GenerateToken()
            };

            tokens.InsertOne(token);
        }

        return token;
    }
}
