using System.Collections.Generic;
using System.Linq;
using fluxel.Components;
using fluxel.Config;
using fluxel.Database;
using fluxel.Models.Other;
using fluxel.Tasks;
using fluXis.Online.API.Models;
using Midori.API.Attributes;
using Midori.API.Components;
using Midori.Networking;

namespace fluxel.API.Controllers;

[Controller]
public class IndexController
{
    private readonly ServerConfig config;
    private readonly UserManager users;
    private readonly TaskRunner tasks;
    private readonly EventManager events;
    private readonly ScoreManager scores;
    private readonly MapManager maps;
    private readonly Statistics stats;
    private readonly ModelTranslator translator;

    public IndexController(ServerConfig config, UserManager users, TaskRunner tasks, EventManager events, ScoreManager scores, MapManager maps, Statistics stats, ModelTranslator translator)
    {
        this.config = config;
        this.users = users;
        this.tasks = tasks;
        this.events = events;
        this.scores = scores;
        this.maps = maps;
        this.stats = stats;
        this.translator = translator;
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

    [HttpRoute("/events")]
    public APIReturn<List<StoredEvent>> Events() => events.GetActive();

    [HttpRoute("/stats")]
    public APIReturn<object> Statistics() => new
    {
        users = users.UserCount - 1,
        online = stats.Online,
        scores = scores.Count,
        mapsets = maps.SetCount
    };

    [HttpRoute("/tasks")]
    [Authenticated(Scopes.DEV)]
    public APIReturn<object> Tasks()
    {
        var tsk = tasks.Queue.ToList();
        return new
        {
            count = tsk.Count,
            tasks = tsk.Select(x => x.ToString())
        };
    }

    [HttpRoute("/team")]
    public APIReturn<object> Team()
    {
        var devs = users.InGroup("dev");
        var staff = users.InGroup("purifier").Concat(users.InGroup("moderators")).OrderBy(x => x.ID).ToList();

        return new
        {
            devs = devs.Select(x => translator.ToAPI(x)),
            staff = staff.DistinctBy(x => x.ID).Select(x => translator.ToAPI(x))
        };
    }

    [HttpRoute("/test/:baller")]
    public APIReturn<object> Testing(HttpServerContext ctx, string baller, [Source(ParameterSource.Query)] string opt = "wah")
        => $"{ctx.EndPoint?.ToString() ?? ""} {baller} {opt}";
}
