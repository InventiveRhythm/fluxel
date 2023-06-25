using System.Text.RegularExpressions;
using fluxel.Components.Maps;
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
            
            return Math.Round(ovr, 2);
        }
    }
    
    [Ignored]
    [JsonProperty("ptr")]
    public double PotentialRating {
        get {
            var ptr = BestScores.Take(30).Sum(score => score.PerformanceRating);
            ptr += RecentScores.Take(10).Sum(score => score.PerformanceRating);
            ptr /= 40f;
            return Math.Round(ptr, 2);
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
                var isranked = RealmAccess.Run(realm => {
                    var set = realm.Find<MapSet>(score.MapShort.MapSet);
                    return set?.Status == 3;
                });

                if (recent.Any(s => s.MapId == score.MapId) || !isranked) continue;
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
                var isranked = RealmAccess.Run(realm => {
                    var set = realm.Find<MapSet>(score.MapShort.MapSet);
                    return set?.Status == 3;
                });
                
                if (best.Any(s => s.MapId == score.MapId) || !isranked) continue;
                best.Add(score);
            }
            
            return best.Take(50).ToList();
        }
    }
    
    [Ignored]
    [JsonProperty("max_combo")]
    public int MaxCombo {
        get {
            var max = 0;
            
            RealmAccess.Run(realm => {
                var scores = realm.All<Score>().Where(s => s.UserId == Id);

                foreach (var score in scores) {
                    if (realm.Find<MapSet>(score.MapShort.MapSet)?.Status == 3) max = Math.Max(max, score.MaxCombo);
                }
            });
            
            return max;
        }
    }
    
    [Ignored]
    [JsonProperty("ranked_score")]
    public int RankedScore {
        get {
            var total = 0;
            
            RealmAccess.Run(realm => {
                var scores = realm.All<Score>().Where(s => s.UserId == Id);

                foreach (var score in scores) {
                    if (realm.Find<MapSet>(score.MapShort.MapSet)?.Status == 3)
                        total += score.TotalScore;
                }
            });
            
            return total;
        }
    }
    
    [Ignored]
    [JsonProperty("ova")]
    public double OverallAccuracy {
        get {
            double acc = 0;
            var count = 0;
            
            RealmAccess.Run(realm => {
                var scores = realm.All<Score>().Where(s => s.UserId == Id);

                foreach (var score in scores) {
                    if (realm.Find<MapSet>(score.MapShort.MapSet)?.Status != 3) continue;
                    
                    acc += Math.Round(score.Accuracy, 2);
                    count++;
                }
            });
            
            if (count == 0) return 0;
            return acc / count;
        }
    }
    
    [Ignored]
    [JsonProperty("rank")]
    public int Rank {
        get {
            if (OverallRating == 0) return 0;
            var rank = 0;
            
            RealmAccess.Run(realm => {
                var users = realm.All<User>().ToList().OrderByDescending(u => u.OverallRating);
                foreach (var user in users) {
                    rank++;
                    if (user.Id == Id) break;
                }
            });
            
            return rank;
        }
    }
    
    [Ignored]
    [JsonProperty("country_rank")]
    public int CountryRank {
        get {
            if (OverallRating == 0) return 0;
            var rank = 0;
            
            RealmAccess.Run(realm => {
                var users = realm.All<User>().ToList().Where(u => u.CountryCode == CountryCode && u.OverallRating > 0).OrderByDescending(u => u.OverallRating);
                foreach (var user in users) {
                    rank++;
                    if (user.Id == Id) break;
                }
            });
            
            return rank;
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