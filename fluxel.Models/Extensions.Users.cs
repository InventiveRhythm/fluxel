using System;
using System.Linq;
using fluxel.Models.Users;
using Microsoft.EntityFrameworkCore;

namespace fluxel.Models;

public static partial class Extensions
{
    extension(DbSet<UserFollow> db)
    {
        public bool IsFollowing(long follower, long followee)
            => db.Find(follower, followee) != null;

        public bool AreMutual(long u1, long u2)
            => db.IsFollowing(u1, u2) && db.IsFollowing(u2, u1);
    }

    extension(DbSet<User> db)
    {
        public User? ByUsername(string name)
            => db.FirstOrDefault(u => string.Equals(u.Username, name, StringComparison.CurrentCultureIgnoreCase));

        public User? BySteamID(ulong id)
            => db.FirstOrDefault(x => x.SteamID == id);

        public User? ByDiscordID(ulong id)
            => db.FirstOrDefault(x => x.DiscordID == id);
    }
}
