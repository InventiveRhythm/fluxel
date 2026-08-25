using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Midori.Database;
using Midori.Logging;
using MongoDB.Bson;

namespace fluxel.Database.Migrations;

public class Migration001SplitAuthFromUser : DatabaseMigration
{
    public override long Version => 1;

    public override Task Perform(IServiceProvider services)
    {
        var db = services.GetRequiredService<IDatabaseProvider>();
        var auth = services.GetRequiredService<AuthManager>();
        var users = db.GetTable<BsonDocument>(UserManager.TABLE_NAME);

        foreach (var document in users.Find(_ => true))
        {
            var id = document["_id"].AsInt64;
            var email = document["email"].AsString;
            var password = document["password"].AsString;

            var data = new BsonDocument();
            data["email"] = email;
            data["password"] = password;
            auth.RegisterAuthMethod(id, "legacy", data);

            document.Remove("email");
            document.Remove("password");
            document.Remove("totp-enabled");
            users.Replace(x => x["_id"] == id, document);
        }

        return Task.CompletedTask;
    }
}
