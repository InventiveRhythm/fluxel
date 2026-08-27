using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Midori.Database;
using Midori.Logging;
using MongoDB.Bson;

namespace fluxel.Database.Migrations;

public class Migration002MoveClubMembershipToUser : DatabaseMigration
{
    public override long Version => 2;

    public override Task Perform(IServiceProvider services)
    {
        var db = services.GetRequiredService<IDatabaseProvider>();
        var clubs = db.GetTable<BsonDocument>(ClubManager.TABLE_NAME);
        var users = services.GetRequiredService<UserManager>();

        foreach (var document in clubs.Find(_ => true))
        {
            var id = document["_id"].AsInt64;
            var members = document["members"].AsBsonArray;
            var ids = members.Select(x => x.AsInt64);

            foreach (var l in ids) users.UpdateLocked(l, x => x.ClubID = id);

            document.Remove("members");
            clubs.Replace(x => x["_id"] == id, document);
        }

        return Task.CompletedTask;
    }
}
