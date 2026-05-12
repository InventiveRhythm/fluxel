using fluxel.Components;
using fluxel.Database;
using fluXis.Online.API.Models.Groups;
using Midori.API.Attributes;
using Midori.API.Components;

namespace fluxel.API.Controllers;

[Controller("/groups")]
public class GroupsController
{
    private readonly GroupManager groups;
    private readonly ModelTranslator translator;

    public GroupsController(GroupManager groups, ModelTranslator translator)
    {
        this.groups = groups;
        this.translator = translator;
    }

    [HttpRoute("/:id")]
    public APIReturn<APIGroup> Get(string id)
    {
        var group = groups.Get(id);

        if (group is null)
            return Returns.NotFound("group");

        return translator.ToAPI(group, true);
    }
}
