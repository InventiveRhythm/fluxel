using fluxel.Database;
using Realms;

namespace fluxel.Components.Users; 

public class UserToken : RealmObject {
    [Indexed]
    public int UserId { get; set; }
    
    [Required]
    [PrimaryKey]
    public string Token { get; set; } = string.Empty;
    
    public static string GenerateToken() {
        var random = new Random();
        var bytes = new byte[32];
        random.NextBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
    
    public static UserToken? GetByToken(string token) {
        return RealmAccess.Run(realm => realm.Find<UserToken>(token));
    }
    
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