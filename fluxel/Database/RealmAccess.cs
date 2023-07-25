using Realms;

namespace fluxel.Database;

public static class RealmAccess {
    private static RealmConfiguration config => new($"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}fluxel.realm") {
        SchemaVersion = 11
    };

    private static Realm realm => Realm.GetInstance(config);

    public static void Run(Action<Realm> action) => write(realm, action);
    public static T Run<T>(Func<Realm, T> func) => write(realm, func);

    private static void write(Realm realm, Action<Realm> func) {
        Transaction? transaction = null;

        try
        {
            if (!realm.IsInTransaction)
                transaction = realm.BeginWrite();

            func(realm);
            transaction?.Commit();
        }
        finally
        {
            transaction?.Dispose();
        }
    }

    private static T write<T>(Realm realm, Func<Realm, T> func) {
        T? result;
        Transaction? transaction = null;

        try
        {
            if (!realm.IsInTransaction)
                transaction = realm.BeginWrite();

            result = func(realm);
            transaction?.Commit();
        }
        finally
        {
            transaction?.Dispose();
        }

        return result;
    }
}
