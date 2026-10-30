using System.Collections.Generic;
using System.Linq;
using fluxel.Components;
using fluxel.Database.Extensions;
using fluxel.Models;
using fluxel.Models.Users;
using fluXis.Online.Collections;
using Microsoft.EntityFrameworkCore;
using Midori.API.Attributes;
using Midori.API.Components;

namespace fluxel.Services.API.Controllers.Users;

[Controller("/users/:id/collections")]
public class UserCollectionsController
{
    private readonly DatabaseContext database;
    private readonly ModelTranslator translator;

    public UserCollectionsController(ModelTranslator translator, DatabaseContext database)
    {
        this.translator = translator;
        this.database = database;
    }

    [Authenticated]
    [HttpRoute("/")]
    public APIReturn<Collection[]> List(User auth, long id)
    {
        if (!database.Users.TryFind(out var user, id))
            return Returns.NotFound("user");

        var list = new List<Collection>();

        if (auth.ID == id)
        {
            var states = database.MapSetUserStates
                                 .Include(x => x.MapSet)
                                 .Include(x => x.MapSet!.Maps)
                                 .Where(x => x.UserID == id && x.Favorite == true).ToArray();

            list.Add(new Collection
            {
                ID = "favorite",
                Name = "Favorite",
                Type = CollectionType.Favorite,
                Owner = translator.ToAPI(user),
                Items =
                [
                    .. states.SelectMany(s => s.MapSet!.Maps.Select(x => new CollectionItem
                    {
                        ID = x.ID.ToString("X5"),
                        Type = CollectionItemType.Online,
                        Map = translator.ToAPI(x)
                    }))
                ]
            });
        }

        return list.ToArray();
    }
}
