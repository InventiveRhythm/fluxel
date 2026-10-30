using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace fluxel.Database.Extensions;

public static class DatabaseExtensions
{
    public static bool TryFind<T>(this DbSet<T> set, [NotNullWhen(true)] out T? t, params object?[]? keyValues) where T : class
    {
        t = set.Find(keyValues);
        return t != null;
    }
}
