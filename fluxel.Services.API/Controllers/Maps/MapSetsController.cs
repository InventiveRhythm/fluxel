using System.ComponentModel.DataAnnotations;
using System.Linq;
using fluxel.Components;
using fluxel.Config;
using fluxel.Database.Extensions;
using fluxel.Models;
using fluxel.Models.Maps;
using fluxel.Models.Maps.Flags;
using fluxel.Models.Users;
using fluXis.Online.API.Models.Maps;
using fluXis.Online.API.Models.Maps.Modding;
using Midori.API.Attributes;
using Midori.API.Components;
using Midori.Networking;
using Newtonsoft.Json;

namespace fluxel.Services.API.Controllers.Maps;

[Controller("/mapsets")]
public class MapSetsController
{
    private readonly DatabaseContext database;
    private readonly ModelTranslator translator;
    private readonly ServerConfig config;

    public MapSetsController(ModelTranslator translator, DatabaseContext database, ServerConfig config)
    {
        this.translator = translator;
        this.database = database;
        this.config = config;
    }

    [Authenticated(Required = false)]
    [HttpRoute("/:id")]
    public APIReturn<APIMapSet> Get(User? auth, long id)
    {
        if (!database.MapSets.TryFind(out var set, id))
            return Returns.NotFound("mapset");

        return translator.ToAPI(set, userid: auth?.ID, mapInclude: MapIncludes.Claims);
    }

    [Authenticated]
    [HttpRoute("/:id", APIMethod.Delete)]
    public APIReturn<object> DeleteMap(User auth, long id)
    {
        if (!database.MapSets.TryFind(out var set, id))
            return Returns.NotFound("mapset");

        // TODO: reimplement MFA
        /*
           if (interaction.User.HasMfa && !interaction.HasValidMfa)
           {
               await interaction.ReplyMessage(HttpStatusCode.Forbidden, "mfa-required");
               return;
           }
         */

        // TODO: admin panel
        if (set.UserID != auth.ID /*&& !auth.IsModerator()*/)
            return Returns.Message(HttpStatusCode.Forbidden, "You are not the creator of this mapset.");
        if (set.Status >= MapSetStatus.Pure)
            return Returns.Message(HttpStatusCode.Forbidden, "Unable to delete a purified mapset.");

        // TODO:
        return Returns.Message(HttpStatusCode.InternalServerError, "TODO: reimplement deletion");
        // maps.RemoveSet(set.ID);
        // return Returns.Okay();
    }

    [HttpRoute("/bundled")]
    public APIReturn<APIMapSet[]> GetBundled()
    {
        var sets = config.BundledSets.Select(x => database.MapSets.Find(x)).OfType<MapSet>().ToArray();
        return sets.Select(x => translator.ToAPI(x)).ToArray();
    }

    // TODO: admin panel
    /*[Authenticated(Scopes.MOD)]
    [HttpRoute("/:id/metadata")]
    public APIReturn<object> UpdateMetadata(User auth, long id, [Source(ParameterSource.Form)] MapSetFlag? flags = null)
    {
        var set = maps.GetSet(id);
        if (set is null) return Returns.NotFound("mapset");

        if (flags.HasValue)
            set.Flags = flags.Value;

        maps.Update(set);
        return Returns.Okay();
    }*/

    #region Favorites

    [Authenticated]
    [HttpRoute("/:id/favorite")]
    public APIReturn<APIMapSetFavoriteState> GetFavoriteState(User auth, long id)
    {
        if (!database.MapSets.TryFind(out var set, id))
            return Returns.NotFound("mapset");

        return new APIMapSetFavoriteState { Favorite = database.MapSetUserStates.Find(set.ID, auth.ID)?.Favorite ?? false };
    }

    // TODO
    /*[Authenticated]
    [HttpRoute("/:id/favorite", APIMethod.Patch)]
    public APIReturn<APIMapSetFavoriteState> UpdateFavoriteState(User auth, long id, [Source(ParameterSource.Body)] APIMapSetFavoriteState payload)
    {
        if (!database.MapSets.TryFind(out var set, id))
            return Returns.NotFound("mapset");

        using (database.EditAndSave())
        {
            var state = database.MapSetUserStates.Find(id, auth.ID);
            state ??= database.MapSetUserStates.Add(new MapSetUserState { MapSetID = set.ID, UserID = auth.ID }).Entity;
            state.Favorite = true;
        }

        var state = maps.HasFavorite(auth.ID, set.ID);

        if (state != payload.Favorite)
        {
            if (payload.Favorite) maps.AddFavorite(auth.ID, set.ID);
            else maps.RemoveFavorite(auth.ID, set.ID);
        }

        if (state != payload.Favorite)
        {
            if (payload.Favorite)
            {
                modules.SendMessage(new UserCollectionMessage(
                    auth.ID,
                    "favorite",
                    set.GetMaps(translator.Cache).Select(m => new CollectionItem
                    {
                        ID = m.ID.ToString("X5"),
                        Type = CollectionItemType.Online,
                        Map = translator.ToAPI(m)
                    }).ToList(),
                    new List<CollectionItem>(),
                    new List<string>()
                ));
            }
            else
            {
                modules.SendMessage(new UserCollectionMessage(
                    auth.ID,
                    "favorite",
                    new List<CollectionItem>(),
                    new List<CollectionItem>(),
                    set.Maps.Select(m => m.ToString("X5")).ToList()
                ));
            }
        }

        return new APIMapSetFavoriteState { Favorite = maps.HasFavorite(auth.ID, set.ID) };
    }*/

    #endregion

    #region Mods

    [HttpRoute("/:id/modding")]
    public APIReturn<APIModdingAction[]> GetModdingActions(long id)
    {
        if (!database.MapSets.TryFind(out var set, id))
            return Returns.NotFound("mapset");

        var actions = database.ModdingActions.Where(x => x.MapSetID == set.ID).ToArray();
        return actions.Select(x => x.ToAPI(database, u => translator.ToAPI(u))).ToArray();
    }

    // TODO
    /*[Authenticated(Scopes.PURIFY)]
    [HttpRoute("/:id/modding", APIMethod.Post)]
    public APIReturn<APIModdingAction> CreateModdingAction(User auth, long id, [Source(ParameterSource.Body)] ActionCreatePayload payload)
    {
        if (!database.MapSets.TryFind(out var set, id))
            return Returns.NotFound("mapset");

        if (set.Status != MapStatus.Pending)
            return Returns.Message(HttpStatusCode.BadRequest, "Not in queue?");
        if (!set.AddModdingEntry(payload.Type!.Value, auth.ID, maps, scores, out var error))
            return Returns.Message(HttpStatusCode.Forbidden, error);

        var action = maps.CreateModAction(set.ID, auth.ID, payload.Type.Value, payload.Content);
        notifyAction(action, set.CreatorID);

        if (set.Status == MapStatus.Pure)
        {
            events.MapPure(set.ID);
            set.GetMaps(cache).ForEach(x =>
            {
                var rating = x.RecalculateRating(maps);
                maps.QuickUpdate(x.ID, m => m.Rating = rating);
            });
        }

        return action.ToAPI(translator);
    }*/

    /*private void notifyAction(ModdingAction action, long owner)
    {
        if (action.Type is APIModdingActionType.Approve or APIModdingActionType.Deny or APIModdingActionType.Note or APIModdingActionType.RequestChanges)
        {
            notifications.Create(new Notification(owner, NotificationType.QueueStatus)
            {
                MapSet = action.MapSetID,
                QueueAction = action.Type
            });
        }

        tasks.Schedule(new MethodTask(() => events.QueueActionCreate(action.ID)));
    }*/

    public class ActionCreatePayload
    {
        [JsonProperty("type")]
        [Required, EnumDataType(typeof(APIModdingActionType))]
        [Range((int)APIModdingActionType.Note, (int)APIModdingActionType.Deny)]
        public APIModdingActionType? Type { get; set; }

        [JsonProperty("content")]
        [Required]
        public string? Content { get; set; }
    }

    #endregion

    #region Queue

    // TODO
    /*[HttpRoute("/queue")]
    public APIReturn<List<APIMapSet>> SearchQueue([Source(ParameterSource.Query)] int limit = 50, [Source(ParameterSource.Query)] int offset = 0)
    {
        var queue = maps.AllQueueSets;
        var count = queue.Count;
        queue = queue.OrderBy(x => x.QueueTime).ToList();

        limit = Math.Clamp(limit, 1, 50);
        queue = queue.Skip(offset).Take(limit).ToList();

        // TODO: pagination
        // interaction.SetPaginationInfo(limit, offset, count, queue.Count);
        return queue.Select(x => translator.ToAPI(x, MapSetInclude.QueueInfo)).ToList();
    }

    [Authenticated]
    [HttpRoute("/:id/submit", APIMethod.Post)]
    public APIReturn<object> SubmitToQueue(User auth, long id)
    {
        var set = maps.GetSet(id);
        if (set is null) return Returns.NotFound("mapset");

        if (set.CreatorID != auth.ID)
            return Returns.Message(HttpStatusCode.Forbidden, "You are not the creator of this mapset.");

        switch (set.Status)
        {
            case >= MapStatus.Pure:
                return Returns.Message(HttpStatusCode.Conflict, "This mapset is already purified.");

            case MapStatus.Pending:
                return Returns.Message(HttpStatusCode.Conflict, "This mapset is already in the queue.");
        }

        if (set.GetMaps(translator.Cache).Any(x => x.Mode is < 4 or > 8))
            return Returns.Message(HttpStatusCode.BadRequest, "A map in this mapset is not between 4 and 8 keys.");

        var inQueueCount = maps.CountInQueue(auth.ID);

        if (inQueueCount >= MapManager.MAX_MAPSETS_IN_QUEUE)
            return Returns.Message(HttpStatusCode.Conflict, $"You have reached the maximum number of mapsets in the queue. ({inQueueCount}/{MapManager.MAX_MAPSETS_IN_QUEUE})");

        set.QueueTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        set.QueueVotes = new List<ModQueueVote>();
        set.Status = MapStatus.Pending;
        maps.Update(set);

        notifyAction(maps.CreateModAction(set.ID, auth.ID, APIModdingActionType.Submitted), set.CreatorID);
        return Returns.Okay();
    }*/

    #endregion

    #region Votes

    // TODO
    /*[Authenticated(Required = false)]
    [HttpRoute("/:id/votes")]
    public APIReturn<APIMapVotes> GetVoteState(User? auth, long id)
    {
        if (!database.MapSets.TryFind(out var set, id))
            return Returns.NotFound("mapset");

        return set.GetVotes(auth?.ID ?? 0);
    }

    [Authenticated]
    [HttpRoute("/:id/votes", APIMethod.Post)]
    public APIReturn<APIMapVotes> UpdateVoteState(User auth, long id, [Source(ParameterSource.Form)] int vote)
    {
        if (!database.MapSets.TryFind(out var set, id))
            return Returns.NotFound("mapset");

        vote = Math.Clamp(vote, -1, 1);
        set.SetVote(auth.ID, vote);
        maps.Update(set);

        return set.GetVotes(auth.ID);
    }*/

    #endregion
}
