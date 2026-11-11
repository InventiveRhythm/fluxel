using System;
using System.Linq;
using fluxel.Models.Maps.Flags;
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
#pragma warning disable CA1862
        public User? ByUsername(string name)
            => db.FirstOrDefault(u => u.Username.ToUpper() == name.ToUpper());
#pragma warning restore CA1862

        public User? BySteamID(ulong id)
            => db.FirstOrDefault(x => x.SteamID == id);

        public User? ByDiscordID(ulong id)
            => db.FirstOrDefault(x => x.DiscordID == id);

        public (long cur, long max) GetUploadLimit(long id, DatabaseContext ctx, long baseCount, long incPerPure, long incPerYear)
        {
            var user = db.Find(id);
            if (user is null) return (0, baseCount);

            var max = baseCount;
            max += incPerPure * ctx.MapSets.Count(x => x.UserID == id && x.Status == MapSetStatus.Pure);

            var release = new DateTime(2023, 05, 13);
            var created = user.CreatedAt;
            if (release > created) created = release;

            var delta = DateTime.Now - created;
            var years = delta.Days / 365f;
            max += (int)Math.Floor(incPerYear * years);

            return (ctx.MapSets.Count(x => x.UserID == id), max);
        }
    }
}
