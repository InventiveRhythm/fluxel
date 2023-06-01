using Realms;

namespace fluxel.Database; 

public static class RealmAccess {
    private static RealmConfiguration Config => new($"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}fluxel.realm") {
        SchemaVersion = 3
    };
    
    private static Realm Realm => Realm.GetInstance(Config);

    public static void Run(Action<Realm> action) => Write(Realm, action);
    
    public static T Run<T>(Func<Realm, T> func) => Write(Realm, func);

    private static void Write(Realm realm, Action<Realm> func) {
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
    
    private static T Write<T>(Realm realm, Func<Realm, T> func) {
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