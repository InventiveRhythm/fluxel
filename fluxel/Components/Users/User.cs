using fluxel.Components.Maps;
using fluxel.Components.Scores;
using fluxel.Database;
using fluxel.Database.Helpers;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace fluxel.Components.Users;

public class User {
    [JsonProperty("id")]
    public long Id { get; set; }

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

    [BsonIgnore]
    [JsonProperty("is_online")]
    public bool IsOnline => GlobalStatistics.GetOnlineUsers.Contains(Id) || Id == 0;

    [BsonIgnore]
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

    [BsonIgnore]
    [JsonProperty("ptr")]
    public double PotentialRating {
        get {
            var best = BestScores.Take(30).Sum(score => score.PerformanceRating);
            var recent = RecentScores.Take(10).Sum(score => score.PerformanceRating);
            return Math.Round((best + recent) / 40f, 2);
        }
    }

    [BsonIgnore]
    [JsonIgnore]
    private List<Score>? recentScores { get; set; }

    [BsonIgnore]
    [JsonIgnore]
    private List<Score>? bestScores { get; set; }

    [BsonIgnore]
    [JsonIgnore]
    public List<Score> RecentScores {
        get {
            if (recentScores != null) return recentScores;

            var scores = ScoreHelper.GetByUser(Id).OrderByDescending(s => s.Time);
            var recent = new List<Score>();

            foreach (var score in scores) {
                if (score.MapInfo.Id == 0) continue;
                /*var isranked = RealmAccess.Run(realm => {
                    var set = realm.Find<MapSet>(score.MapShort.MapSet);
                    return set?.Status == 3;
                });*/

                var isranked = true;

                if (recent.Any(s => s.MapId == score.MapId) || !isranked) continue;
                recent.Add(score);
            }

            return recentScores = recent.Take(30).ToList();
        }
    }

    [BsonIgnore]
    [JsonIgnore]
    public List<Score> BestScores {
        get {
            if (bestScores != null) return bestScores;

            var scores = ScoreHelper.GetByUser(Id).OrderByDescending(s => s.PerformanceRating);
            var best = new List<Score>();

            foreach (var score in scores) {
                if (score.MapInfo.Id == 0) continue;
                /*var isranked = RealmAccess.Run(realm => {
                    var set = realm.Find<MapSet>(score.MapShort.MapSet);
                    return set?.Status == 3;
                });*/

                var isranked = true;

                if (best.Any(s => s.MapId == score.MapId) || !isranked) continue;
                best.Add(score);
            }

            return bestScores = best.Take(50).ToList();
        }
    }

    [BsonIgnore]
    [JsonProperty("max_combo")]
    public int MaxCombo {
        get {
            var max = 0;

            var scores = ScoreHelper.GetByUser(Id);

            foreach (var score in scores)
            {
                if (score.MapInfo.Id == 0) continue;
                // if (realm.Find<MapSet>(score.MapShort.MapSet)?.Status != 3) continue;
                max = Math.Max(max, score.MaxCombo);
            }

            return max;
        }
    }

    [BsonIgnore]
    [JsonProperty("ranked_score")]
    public int RankedScore {
        get {
            var total = 0;

            var scores = ScoreHelper.GetByUser(Id);

            foreach (var score in scores)
            {
                if (score.MapInfo.Id == 0) continue;

                // if (realm.Find<MapSet>(score.MapShort.MapSet)?.Status != 3) continue;
                total += score.TotalScore;
            }

            return total;
        }
    }

    [BsonIgnore]
    [JsonProperty("ova")]
    public double OverallAccuracy {
        get {
            double acc = 0;
            var count = 0;

            var scores = ScoreHelper.GetByUser(Id);

            foreach (var score in scores) {
                // if (realm.Find<MapSet>(score.MapShort.MapSet)?.Status != 3) continue;

                acc += Math.Round(score.Accuracy, 2);
                count++;
            }

            if (count == 0) return 0;
            return acc / count;
        }
    }

    [BsonIgnore]
    [JsonProperty("rank")]
    public int Rank {
        get {
            if (OverallRating == 0) return 0;
            var rank = 0;

            var users = UserHelper.All.OrderByDescending(u => u.OverallRating);

            foreach (var user in users) {
                rank++;
                if (user.Id == Id) break;
            }

            return rank;
        }
    }

    [BsonIgnore]
    [JsonProperty("country_rank")]
    public int CountryRank {
        get {
            if (OverallRating == 0) return 0;
            var rank = 0;

            var countryUsers = UserHelper.All.Where(u => u.CountryCode == CountryCode).OrderByDescending(u => u.OverallRating);

            foreach (var user in countryUsers) {
                rank++;
                if (user.Id == Id) break;
            }

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

    public class UserMaps
    {
        private User user { get; }

        [JsonProperty("ranked")]
        public List<MapSet> Ranked => MapSetHelper.GetByCreator(user.Id).Where(s => s.Status == 3).ToList();

        [JsonProperty("unranked")]
        public List<MapSet> Unranked => MapSetHelper.GetByCreator(user.Id).Where(s => s.Status != 3).ToList();

        [JsonProperty("guest_diffs")]
        public List<MapSet> GuestDiffs => MapSetHelper.All.Where(s => s.CreatorId != user.Id && s.MapsList.Any(m => m.MapperId == user.Id)).ToList();

        public UserMaps(User user) {
            this.user = user;
        }
    }

    [Obsolete]
    public static User? FindById(int id) => MongoDatabase.GetCollection<User>("users").Find(u => u.Id == id).FirstOrDefault();
}
