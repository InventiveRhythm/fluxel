using System.Collections.Generic;
using System.Linq;
using fluxel.Models.Groups;
using Midori.Database;

namespace fluxel.Database;

public class GroupManager
{
    private readonly IDatabaseTable<Group> groups;

    public List<Group> All => groups.Find(m => true).ToList();

    public GroupManager(IDatabaseProvider db)
    {
        groups = db.GetTable<Group>("groups");
    }

    public void Add(Group group) => groups.Add(group);
    public Group? Get(string id) => groups.Find(m => m.ID == id).FirstOrDefault();
}
