using System.Linq;
using fluxel.Components;
using fluxel.Components.IPC;
using fluxel.Config;
using fluxel.Models;
using fluXis.Online.API.Models;
using Microsoft.EntityFrameworkCore;
using Midori.API.Attributes;
using Midori.API.Components;
using Midori.Networking;

namespace fluxel.Services.API.Controllers;

[Controller]
public class IndexController
{
    private readonly ServerConfig config;
    private readonly DatabaseContext database;
    private readonly ModelTranslator translator;
    private readonly Valkey valkey;

    public IndexController(ServerConfig config, DatabaseContext database, ModelTranslator translator, Valkey valkey)
    {
        this.config = config;
        this.database = database;
        this.translator = translator;
        this.valkey = valkey;
    }

    [HttpRoute("/")]
    public APIReturn<object> Index()
        => Returns.Message(HttpStatusCode.OK, "Welcome to fluxel, the API for fluXis! You can see the API docs at https://fluxis.flux.moe/wiki/api");

    [HttpRoute("/config")]
    public APIReturn<APIConfig> Configuration() => new APIConfig
    {
        AssetsUrl = config.Urls.Assets,
        WebsiteUrl = config.Urls.Website,
        WikiUrl = config.Urls.Website + "/wiki"
    };

    [HttpRoute("/stats")]
    public APIReturn<object> Statistics() => new
    {
        users = database.Users.Count() - 1,
        online = valkey.Database.SetMembers(Valkey.UsersOnline).Length, // ListCount doesn't wanna work
        scores = database.Scores.Count(),
        mapsets = database.MapSets.Count()
    };

    [HttpRoute("/team")]
    public APIReturn<object> Team()
    {
        var devs = database.GroupMembers
                           .Include(x => x.User)
                           .Where(x => x.GroupID == "dev")
                           .ToArray();

        var staff = database.GroupMembers
                            .Include(x => x.User)
                            .Where(x => x.GroupID == "purifier" || x.GroupID == "moderators")
                            .OrderBy(x => x.UserID)
                            .ToArray();

        return new
        {
            devs = devs.Select(x => translator.ToAPI(x.User!)),
            staff = staff.DistinctBy(x => x.UserID)
                         .Select(x => translator.ToAPI(x.User!))
        };
    }
}
