using System.Text.RegularExpressions;
using fluxel.Components.Users;
using MongoDB.Driver;

namespace fluxel.Database.Helpers;

public static class UserHelper
{
    private static IMongoCollection<User> users => MongoDatabase.GetCollection<User>("users");

    public static List<User> All => users.Find(u => true).ToList();
    public static long Count => users.CountDocuments(u => true);

    public static void Add(User user) => users.InsertOne(user);

    public static User? Get(long id) => users.Find(u => u.Id == id).FirstOrDefault();
    public static User? Get(string name) => users.Find(u => string.Equals(u.Username, name, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();

    public static void Update(User user) => users.ReplaceOne(u => u.Id == user.Id, user);

    public static long NextId {
        get
        {
            var user = users.Find(u => true).SortByDescending(u => u.Id).FirstOrDefault();
            return user?.Id + 1 ?? 1;
        }
    }

    public static bool ValidUsername(this string username) => Regex.IsMatch(username, "^[a-zA-Z0-9_]{3,16}$");
    public static bool ValidDisplayName(this string displayName) => Regex.IsMatch(displayName, "^[a-zA-Z0-9_ ]{1,20}$");

    public static bool UsernameExists(this string username) => users.Find(u => string.Equals(u.Username, username, StringComparison.CurrentCultureIgnoreCase)).Any();
}
