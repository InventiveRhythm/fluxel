/*
using System.Collections.Generic;
using System.Linq;
using fluxel.Components;
using fluxel.Components.Tasks;
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
            tasks = tsk.Select(x => new
            {
                name = x.Task.Name,
                interval = x.Interval,
                next = x.NextRun
            })
        };
    }

    [HttpRoute("/test/:baller")]
    public APIReturn<object> Testing(HttpServerContext ctx, string baller, [Source(ParameterSource.Query)] string opt = "wah")
        => $"{ctx.EndPoint?.ToString() ?? ""} {baller} {opt}";
}
*/
