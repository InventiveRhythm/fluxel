using System.Collections.Generic;
using System.Linq;
using fluxel.Components;
using fluxel.Database;
using fluxel.Models.Maps;
using fluxel.Models.Users;
using fluXis.Online.API.Models.Users;
using fluXis.Online.Collections;
using Midori.API.Attributes;
using Midori.API.Components;
using Midori.Networking;

namespace fluxel.API.Controllers.Users;

[Controller("/users/:id/collections")]
public class UserCollectionsController
{
    private readonly ModelTranslator translator;
    private readonly UserManager users;
    private readonly MapManager maps;

    public UserCollectionsController(ModelTranslator translator, UserManager users, MapManager maps)
    {
        this.translator = translator;
        this.users = users;
        this.maps = maps;
    }

    [Authenticated]
    [HttpRoute("/")]
    public APIReturn<List<Collection>> List(User auth, long id)
    {
        if (auth.ID != id)
            return Returns.Message(HttpStatusCode.Forbidden, "You cannot fetch the collections of another user.");

        var user = users.Get(id);

        var favorite = new Collection
        {
            ID = "favorite",
            Name = "Favorite",
            Type = CollectionType.Favorite,
            Owner = user != null ? translator.ToAPI(user) : APIUser.CreateUnknown(id),
            Items = maps.AllFavoriteByUser(id)
                        .Select(maps.GetSet).OfType<MapSet>()
                        .SelectMany(set => set.GetMaps(translator.Cache).Select(map => translator.ToAPI(map, set: set, userid: id)))
                        .Select(x => new CollectionItem
                        {
                            ID = x.ID.ToString("X5"),
                            Type = CollectionItemType.Online,
                            Map = x
                        }).ToList()
        };

        return new List<Collection> { favorite };
    }
}
