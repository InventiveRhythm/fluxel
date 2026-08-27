using System;
using fluxel.Models;
using fluxel.Models.Clubs;
using Microsoft.EntityFrameworkCore;
using Midori.Utils;

namespace fluxel.Database;

public class DatabaseContext : DbContext
{
    public DbSet<Counter> Counters => Set<Counter>();
    public DbSet<Club> Clubs => Set<Club>();

    public DatabaseContext(DbContextOptions<DatabaseContext> opt)
        : base(opt)
    {
    }

    public IDisposable EditAndSave() => new InvokeOnDisposal(() => SaveChanges());
}
