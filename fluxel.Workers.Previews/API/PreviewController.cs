using fluxel.API;
using fluxel.Database;
using fluxel.Tasks;
using fluxel.Workers.Previews.Tasks;
using Midori.API.Attributes;
using Midori.API.Components;

namespace fluxel.Workers.Previews.API;

[Controller("/workers/previews")]
public class PreviewController
{
    private readonly MapManager maps;
    private readonly TaskRunner tasks;

    public PreviewController(TaskRunner tasks, MapManager maps)
    {
        this.tasks = tasks;
        this.maps = maps;
    }

    [Authenticated(Scopes.DEV)]
    [HttpRoute("/regenerate", APIMethod.Post)]
    public APIReturn<object> RegenerateAll()
    {
        tasks.Schedule(new RegeneratePreviewsBulkTask());
        return Returns.Okay();
    }

    [Authenticated(Scopes.DEV)]
    [HttpRoute("/regenerate/:id", APIMethod.Post)]
    public APIReturn<object> RegenerateAll(long id)
    {
        if (maps.GetSet(id) == null)
            return Returns.NotFound();

        tasks.Schedule(new GeneratePreviewTask(id));
        return Returns.Okay();
    }
}
