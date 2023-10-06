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
    public string Password { get; init; } = "";

    [JsonProperty("username")]
    public string Username { get; set; } = "";

    [JsonProperty("displayname")]
    public string DisplayName { get; set; } = "";

    [JsonIgnore]
    public string Email { get; set; } = "";

    [JsonProperty("aboutme")]
    public string AboutMe { get; set; } = "";

    [JsonProperty("role")]
    public int Role { get; set; }

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
    public bool IsOnline => GlobalStatistics.GetOnlineUsers.Contains(Id) || Id == 0;

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
            var best = BestScores.Take(30).Sum(score => score.PerformanceRating);
            var recent = RecentScores.Take(10).Sum(score => score.PerformanceRating);
            return Math.Round((best + recent) / 40f, 2);
        }
    }

    [Ignored]
    [JsonIgnore]
    private List<Score>? recentScores { get; set; }

    [Ignored]
    [JsonIgnore]
    private List<Score>? bestScores { get; set; }

    [Ignored]
    [JsonIgnore]
    public List<Score> RecentScores {
        get {
            if (recentScores != null) return recentScores;

            var scores = RealmAccess.Run(realm => realm.All<Score>().Where(s => s.UserId == Id))
                .ToList().OrderByDescending(s => s.Time);

            var recent = new List<Score>();

            foreach (var score in scores) {
                if (score.MapInfo.Id == 0) continue;
                var isranked = RealmAccess.Run(realm => {
                    var set = realm.Find<MapSet>(score.MapShort.MapSet);
                    return set?.Status == 3;
                });

                isranked = true;

                if (recent.Any(s => s.MapId == score.MapId) || !isranked) continue;
                recent.Add(score);
            }

            return recentScores = recent.Take(30).ToList();
        }
    }

    [Ignored]
    [JsonIgnore]
    public List<Score> BestScores {
        get {
            if (bestScores != null) return bestScores;

            var scores = RealmAccess.Run(realm => realm.All<Score>().Where(s => s.UserId == Id))
                .ToList().OrderByDescending(s => s.PerformanceRating);

            var best = new List<Score>();

            foreach (var score in scores) {
                if (score.MapInfo.Id == 0) continue;
                var isranked = RealmAccess.Run(realm => {
                    var set = realm.Find<MapSet>(score.MapShort.MapSet);
                    return set?.Status == 3;
                });

                isranked = true;

                if (best.Any(s => s.MapId == score.MapId) || !isranked) continue;
                best.Add(score);
            }

            return bestScores = best.Take(50).ToList();
        }
    }

    [Ignored]
    [JsonProperty("max_combo")]
    public int MaxCombo {
        get {
            var max = 0;

            RealmAccess.Run(realm => {
                var scores = realm.All<Score>().Where(s => s.UserId == Id);

                foreach (var score in scores)
                {
                    if (score.MapInfo.Id == 0) continue;
                    // if (realm.Find<MapSet>(score.MapShort.MapSet)?.Status != 3) continue;
                    max = Math.Max(max, score.MaxCombo);
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

                foreach (var score in scores)
                {
                    if (score.MapInfo.Id == 0) continue;

                    // if (realm.Find<MapSet>(score.MapShort.MapSet)?.Status != 3) continue;
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
                    // if (realm.Find<MapSet>(score.MapShort.MapSet)?.Status != 3) continue;

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
            DisplayName = DisplayName,
            CountryCode = CountryCode,
            Role = Role,
            Socials = Socials
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

    public class UserMaps
    {
        private User user { get; }

        [JsonProperty("ranked")]
        public List<MapSet> Ranked => RealmAccess.Run(realm => realm.All<MapSet>().Where(s => s.CreatorId == user.Id && s.Status == 3).ToList());

        [JsonProperty("unranked")]
        public List<MapSet> Unranked => RealmAccess.Run(realm => realm.All<MapSet>().Where(s => s.CreatorId == user.Id && s.Status != 3).ToList());

        [JsonProperty("guest_diffs")]
        public List<MapSet> GuestDiffs => RealmAccess.Run(realm => realm.All<MapSet>().ToList().Where(s => s.CreatorId != user.Id && s.MapsList.Any(m => m.MapperId == user.Id))).ToList();

        public UserMaps(User user) {
            this.user = user;
        }
    }

    public static bool ValidUsername(string username) => Regex.IsMatch(username, "^[a-zA-Z0-9_]{3,16}$");
    public static int Count() => RealmAccess.Run(realm => realm.All<User>().Count());
    public static User? FindById(int id) => RealmAccess.Run(realm => realm.Find<User>(id));
    public static User? FindByUsername(string username) => RealmAccess.Run(realm => realm.All<User>().FirstOrDefault(u => u.Username == username));

    public static int GetNextId() {
        return RealmAccess.Run(realm => {
            var users = realm.All<User>();

            var max = 0;

            foreach (var user in users) {
                if (user.Id > max) {
                    max = user.Id;
                }
            }

            return !users.Any() ? 1 : max + 1;
        });
    }
}
