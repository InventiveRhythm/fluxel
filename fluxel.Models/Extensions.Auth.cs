using System;
using System.Linq;
using System.Threading.Tasks;
using fluxel.Models.Auth;
using Microsoft.EntityFrameworkCore;
using Midori.Utils;

namespace fluxel.Models;

public static partial class Extensions
{
    extension(DbSet<AuthSession> db)
    {
        public async Task<AuthSession> Create(long uid, string ip, string ua)
        {
            var (country, region, city) = await Utilities.GetIPInfo(ip);
            var token = RandomizeUtils.GenerateRandomString(32, CharacterType.AllOfIt);

            return (await db.AddAsync(new AuthSession
            {
                UserAgent = ua,
                UserID = uid,
                Country = country,
                IPAddress = ip,
                Location = string.IsNullOrWhiteSpace(region) ? null : $"{region}/{city ?? string.Empty}".TrimEnd('/'),
                Token = token
            })).Entity;
        }

        public AuthSession? FindFromGame(long uid, bool update)
        {
            var session = db.FirstOrDefault(x => x.UserID == uid && x.UserAgent == "fluXis");
            if (session is null) return null;

            if (update)
                session.LastActivity = DateTime.UtcNow;

            return session;
        }

        public AuthSession? FindByIP(long uid, string ip, bool update)
        {
            var session = db.FirstOrDefault(x => x.UserID == uid && x.IPAddress == ip && !x.UserAgent.StartsWith("fluXis"));
            if (session is null) return null;

            if (update)
                session.LastActivity = DateTime.UtcNow;

            return session;
        }
    }
}
