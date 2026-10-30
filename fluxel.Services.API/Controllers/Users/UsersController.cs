using System;
using System.Linq;
using fluxel.Components;
using fluxel.Components.IPC;
using fluxel.Models;
using fluxel.Models.Users;
using fluXis.Online.API.Models.Users;
using fluXis.Online.API.Responses.Users;
using Midori.API.Attributes;
using Midori.API.Components;

namespace fluxel.Services.API.Controllers.Users;

[Controller("/users")]
public class UsersController
{
    private readonly DatabaseContext database;
    private readonly ModelTranslator translator;
    private readonly Valkey valkey;

    public UsersController(ModelTranslator translator, DatabaseContext database, Valkey valkey)
    {
        this.translator = translator;
        this.database = database;
        this.valkey = valkey;
    }

    // TODO: move this to atlas
    [Authenticated(Required = false)]
    [HttpRoute("/")]
    public APIReturn<APIUser[]> List(
        User? auth,
        [Source(ParameterSource.Query)] int limit = 50,
        [Source(ParameterSource.Query)] int offset = 0)
    {
        limit = Math.Clamp(limit, 1, 100);

        var users = database.Users.Skip(offset).Take(limit).ToArray();
        return users.Select(x => translator.ToAPI(x)).ToArray();
    }

    [HttpRoute("/online")]
    public APIReturn<OnlineUsers> Online()
    {
        var values = valkey.Database.SetMembers(Valkey.UsersOnline);
        var ids = values.Select(x => (long)x).ToArray();
        var users = ids.Select(x => database.Users.Find(x)).Select(x => translator.ToAPI(x)).OfType<APIUser>().ToArray();
        return new OnlineUsers(users.Length, users);
    }
}
