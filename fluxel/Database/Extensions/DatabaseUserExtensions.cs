using System;
using System.Collections.Generic;
using System.Linq;
using fluxel.Models.Users;
using Microsoft.EntityFrameworkCore;

namespace fluxel.Database.Extensions;

public static class DatabaseUserExtensions
{
    #region Discord

    public static List<UserDiscordConnection> GetExpiring(this DbSet<UserDiscordConnection> set)
        => [.. set.Where(x => x.Expire < DateTimeOffset.Now + TimeSpan.FromDays(1))];

    public static void AddOrUpdate(this DbSet<UserDiscordConnection> set, UserDiscordConnection conn)
    {
        /*var exists = GetDiscord(conn.ID) != null;

        if (exists)
            discord.Replace(c => c.ID == conn.ID, conn);
        else
            discord.Add(conn);*/
    }

    #endregion
}
