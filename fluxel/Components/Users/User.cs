using System.Text.RegularExpressions;
using fluxel.Database;
using Newtonsoft.Json;
using Realms;

namespace fluxel.Components.Users; 

public class User : RealmObject {
    [PrimaryKey]
    [JsonProperty("id")]
    public int Id { get; set; }
    
    [JsonIgnore]
    public string Password { get; set; } = "";
    
    [JsonProperty("username")]
    public string Username { get; set; } = "";
    
    [JsonIgnore]
    public string Email { get; set; } = "";
    
    [JsonProperty("aboutme")]
    public string AboutMe { get; set; } = "";
    
    [JsonProperty("role")]
    public int Role { get; set; } = 0;
    
    [JsonProperty("country")]
    public string CountryCode { get; set; } = string.Empty;
    
    [JsonProperty("social")]
    public UserSocials Socials { get; set; } = new();
    
    [JsonProperty("created")]
    public long CreatedAt { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    
    [JsonProperty("lastlogin")]
    public long LastLogin { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    
    [Ignored]
    [JsonProperty("is_online")]
    public bool IsOnline => Stats.GetOnlineUsers.Contains(Id);

    public UserShort ToShort() {
        return new UserShort {
            Id = Id,
            Username = Username
        };
    }
    
    public static bool UsernameExists(string username) {
        return RealmAccess.Run(realm => realm.All<User>().Any(u => u.Username.ToLowerInvariant() == username.ToLowerInvariant()));
    }
    
    public static bool ValidUsername(string username) {
        return Regex.IsMatch(username, @"^[a-zA-Z0-9_]{3,16}$");
    }
    
    public static int Count() {
        return RealmAccess.Run(realm => realm.All<User>().Count());
    }
    
    public static User? FindById(int id) {
        return RealmAccess.Run(realm => realm.Find<User>(id));
    }
    
    public static User? FindByUsername(string username) {
        return RealmAccess.Run(realm => realm.All<User>().FirstOrDefault(u => u.Username == username));
    }
    
    public static int GetNextId() {
        return RealmAccess.Run(realm => {
            var users = realm.All<User>();
            
            int max = 0;
            
            foreach (var user in users) {
                if (user.Id > max) {
                    max = user.Id;
                }
            }
            
            return !users.Any() ? 1 : max + 1;
        });
    }
}