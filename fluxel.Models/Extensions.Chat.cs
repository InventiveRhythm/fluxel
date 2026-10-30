using fluxel.Models.Chat;
using Microsoft.EntityFrameworkCore;

namespace fluxel.Models;

public partial class Extensions
{
    extension(DbSet<ChatChannelMember> db)
    {
        public bool IsMemberOf(string channel, long user)
            => db.Find(channel, user) != null;
    }
}
