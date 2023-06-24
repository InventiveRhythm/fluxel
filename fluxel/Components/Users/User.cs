using System.Text.RegularExpressions;
using fluxel.Components.Scores;
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
    public string? CountryCode { get; set; } = string.Empty;
    
    [JsonProperty("social")]
    public UserSocials Socials { get; set; } = new();
    
    [JsonProperty("created")]
    public long CreatedAt { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    
    [JsonProperty("lastlogin")]
    public long LastLogin { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    
    [Ignored]
    [JsonProperty("is_online")]
    public bool IsOnline => Stats.GetOnlineUsers.Contains(Id);

    [Ignored]
    [JsonProperty("ovr")]
    public double OverallRating {
        get {
            var ovr = 0d;
            var count = 0;
            
            foreach (var score in BestScores) {
                ovr += score.PerformanceRating * Math.Pow(.9f, count);
                count++;
            }
            
            return ovr;
        }
    }
    
    [Ignored]
    [JsonProperty("ptr")]
    public double PotentialRating {
        get {
            var ptr = BestScores.Take(30).Sum(score => score.PerformanceRating);
            ptr += RecentScores.Take(10).Sum(score => score.PerformanceRating);
            ptr /= 40f;
            return ptr;
        }
    }
    
    [Ignored]
    [JsonProperty("recent_scores")]
    public List<Score> RecentScores {
        get {
            var scores = RealmAccess.Run(realm => realm.All<Score>().Where(s => s.UserId == Id))
                .ToList().OrderByDescending(s => s.Time);
            
            var recent = new List<Score>();
            
            foreach (var score in scores) {
                if (recent.Any(s => s.MapId == score.MapId)) continue;
                recent.Add(score);
            }
            
            return recent.Take(30).ToList();
        }
    }
    
    [Ignored]
    [JsonProperty("best_scores")]
    public List<Score> BestScores {
        get {
            var scores = RealmAccess.Run(realm => realm.All<Score>().Where(s => s.UserId == Id))
                .ToList().OrderByDescending(s => s.PerformanceRating);
            
            var best = new List<Score>();
            
            foreach (var score in scores) {
                if (best.Any(s => s.MapId == score.MapId)) continue;
                best.Add(score);
            }
            
            return best.Take(50).ToList();
        }
    }

    public UserShort ToShort() {
        return new UserShort {
            Id = Id,
            Username = Username,
            CountryCode = CountryCode
        };
    }
    
    public static bool UsernameExists(string username) {
        return RealmAccess.Run(realm => {
            var users = realm.All<User>();
            
            foreach (var user in users) {
                var uname = user.Username.ToLower();
                if (uname == username.ToLower()) {
                    return user;
                }
            }
            
            return null;
        }) != null;
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