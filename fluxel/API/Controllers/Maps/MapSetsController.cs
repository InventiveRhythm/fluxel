using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using fluxel.Components;
using fluxel.Config;
using fluxel.Database;
using fluxel.Database.Extensions;
using fluxel.Models.Maps;
using fluxel.Models.Maps.Modding;
using fluxel.Models.Users;
using fluxel.Modules;
using fluxel.Modules.Messages;
using fluxel.Search;
using fluxel.Search.Filters;
using fluxel.Tasks;
using fluxel.Tasks.Other;
using fluxel.Utils;
using fluXis.Online.API.Models.Maps;
using fluXis.Online.API.Models.Maps.Modding;
using fluXis.Online.Collections;
using Midori.API.Attributes;
using Midori.API.Components;
using Midori.Networking;
using Newtonsoft.Json;
using osu.Framework.Extensions.EnumExtensions;

namespace fluxel.API.Controllers.Maps;

[Controller("/mapsets")]
public class MapSetsController
{
    private readonly MapManager maps;
    private readonly ScoreManager scores;
    private readonly ModelTranslator translator;
    private readonly ServerConfig config;
    private readonly ServerEvents events;
    private readonly ModuleManager modules;
    private readonly TaskRunner tasks;
    private readonly RequestCache cache;

    public MapSetsController(MapManager maps, ModelTranslator translator, ServerConfig config, ModuleManager modules, ScoreManager scores, ServerEvents events, TaskRunner tasks, RequestCache cache)
    {
        this.maps = maps;
        this.translator = translator;
        this.config = config;
        this.modules = modules;
        this.scores = scores;
        this.events = events;
        this.tasks = tasks;
        this.cache = cache;
    }

    [HttpRoute("/")]
    public APIReturn<List<APIMapSet>> Search(
        [Source(ParameterSource.Query)] int limit = 50,
        [Source(ParameterSource.Query)] int offset = 0,
        [Source(ParameterSource.Query, "q")] string query = "",
        [Source(ParameterSource.Query)] int? status = null)
    {
        limit = Math.Clamp(limit, 1, 50);

        var all = translator.Cache.MapSets.All;
        translator.Cache.Maps.EnsureAll();

        var filter = new MapSetSearchFilter(translator.Cache);
        SearchParser.Parse<MapSetSearchFilter, MapSet>(filter, query);

        if (status.HasValue)
        {
            if (status <= 10)
            {
                if (!Enum.IsDefined((MapStatus)status.Value))
                    return Returns.Message(HttpStatusCode.BadRequest, "Invalid status provided.");

                filter.Status = status switch
                {
                    -1 or 0 => StatusFlags.Unsubmitted,
                    1 => StatusFlags.Pending,
                    2 => StatusFlags.Impure,
                    3 or 4 => StatusFlags.Pure,
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
            else
                filter.Status = null;
        }

        var sets = all.Where(s => filter.Match(s) && !s.InternalFlags.HasFlagFast(InternalSetFlags.ShadowBan))
                      .OrderByDescending(x => filter.Status == StatusFlags.Pure ? x.DateRanked : x.Submitted).Skip(offset).Take(limit).Select(x => translator.ToAPI(x)).ToList();

        // TODO: pagination
        // interaction.SetPaginationInfo(limit, offset, all.Count, sets.Count);
        return sets;
    }

    [Authenticated(Required = false)]
    [HttpRoute("/:id")]
    public APIReturn<APIMapSet> Get(User? auth, long id)
    {
        var set = maps.GetSet(id);
        if (set is null) return Returns.NotFound("mapset");

        return translator.ToAPI(set, userid: auth?.ID, mapInclude: MapIncludes.Claims);
    }

    [HttpRoute("/:id/description")]
    public APIReturn<string> GetDescription(long id)
    {
        var set = maps.GetSet(id);
        if (set is null) return Returns.NotFound("mapset");

        return set.Description;
    }

    [Authenticated]
    [HttpRoute("/:id/description", APIMethod.Patch)]
    public APIReturn<object> EditDescription(User auth, long id, [Source(ParameterSource.Form)] string content)
    {
        var set = maps.GetSet(id);
        if (set is null) return Returns.NotFound("mapset");

        if (set.CreatorID != auth.ID && !auth.IsModerator())
            return Returns.Message(HttpStatusCode.Forbidden, "You are not the creator of this mapset.");

        int descLimit = config.Limits.MaxDescChar;

        if (content.Length > descLimit)
            return Returns.Message(HttpStatusCode.Forbidden, $"Description cannot exceed {descLimit} characters.");

        set.Description = HtmlUtils.SanitizeHtml(content);
        maps.Update(set);

        return Returns.Okay();
    }

    [Authenticated]
    [HttpRoute("/:id", APIMethod.Delete)]
    public APIReturn<object> DeleteMap(User auth, long id)
    {
        var set = maps.GetSet(id);
        if (set is null) return Returns.NotFound("mapset");

        // TODO: reimplement MFA
        /*
           if (interaction.User.HasMfa && !interaction.HasValidMfa)
           {
               await interaction.ReplyMessage(HttpStatusCode.Forbidden, "mfa-required");
               return;
           }
         */

        if (set.CreatorID != auth.ID && !auth.IsModerator())
            return Returns.Message(HttpStatusCode.Forbidden, "You are not the creator of this mapset.");
        if (set.Status >= MapStatus.Pure)
            return Returns.Message(HttpStatusCode.Forbidden, "Unable to delete a purified mapset.");

        maps.RemoveSet(set.ID);
        return Returns.Okay();
    }

    [HttpRoute("/bundled")]
    public APIReturn<List<APIMapSet>> GetBundled()
        => config.BundledSets.Select(translator.Cache.MapSets.Get).OfType<MapSet>()
                 .Select(x => translator.ToAPI(x)).ToList();

    [Authenticated(Scopes.MOD)]
    [HttpRoute("/:id/metadata")]
    public APIReturn<object> UpdateMetadata(User auth, long id, [Source(ParameterSource.Form)] MapSetFlag? flags = null)
    {
        var set = maps.GetSet(id);
        if (set is null) return Returns.NotFound("mapset");

        if (flags.HasValue)
            set.Flags = flags.Value;

        maps.Update(set);
        return Returns.Okay();
    }

    [HttpRoute("/:id/download")]
    [ReturnsMime("application/zip")]
    public APIReturn<Stream> Download(long id)
    {
        var set = maps.GetSet(id);
        if (set is null) return Returns.NotFound("mapset");

        var path = Assets.GetPathForAsset(AssetType.Map, set.ID.ToString());

        if (!File.Exists(path))
            return Returns.Message(HttpStatusCode.InternalServerError, "Unable to find file on server.");

        return Assets.GetAssetStream(AssetType.Map, set.ID.ToString()) ?? throw new Exception($"Failed to open asset stream for mapset {set.ID}.");
    }

    #region Favorites

    [Authenticated]
    [HttpRoute("/:id/favorite")]
    public APIReturn<APIMapSetFavoriteState> GetFavoriteState(User auth, long id)
    {
        var set = maps.GetSet(id);
        if (set is null) return Returns.NotFound("mapset");

        return new APIMapSetFavoriteState { Favorite = maps.HasFavorite(auth.ID, set.ID) };
    }

    [Authenticated]
    [HttpRoute("/:id/favorite", APIMethod.Patch)]
    public APIReturn<APIMapSetFavoriteState> UpdateFavoriteState(User auth, long id, [Source(ParameterSource.Body)] APIMapSetFavoriteState payload)
    {
        var set = maps.GetSet(id);
        if (set is null) return Returns.NotFound("mapset");

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
    }

    #endregion

    #region Mods

    [HttpRoute("/:id/modding")]
    public APIReturn<List<APIModdingAction>> GetModdingActions(long id)
    {
        var set = maps.GetSet(id);
        if (set is null) return Returns.NotFound("mapset");

        var actions = maps.GetModActionsFromSet(id);
        return actions.Select(x => x.ToAPI(translator)).ToList();
    }

    [Authenticated(Scopes.PURIFY)]
    [HttpRoute("/:id/modding", APIMethod.Post)]
    public APIReturn<APIModdingAction> CreateModdingAction(User auth, long id, [Source(ParameterSource.Body)] ActionCreatePayload payload)
    {
        var set = maps.GetSet(id);
        if (set is null) return Returns.NotFound("mapset");

        if (set.Status != MapStatus.Pending)
            return Returns.Message(HttpStatusCode.BadRequest, "Not in queue?");
        if (!set.AddModdingEntry(payload.Type!.Value, auth.ID, maps, scores, out var error))
            return Returns.Message(HttpStatusCode.Forbidden, error);

        var action = maps.CreateModAction(set.ID, auth.ID, payload.Type.Value, payload.Content);
        notifyAction(action);

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
    }

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

    [HttpRoute("/queue")]
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

        notifyAction(maps.CreateModAction(set.ID, auth.ID, APIModdingActionType.Submitted));
        return Returns.Okay();
    }

    #endregion

    #region Votes

    [Authenticated(Required = false)]
    [HttpRoute("/:id/votes")]
    public APIReturn<APIMapVotes> GetVoteState(User? auth, long id)
    {
        var set = maps.GetSet(id);
        if (set is null) return Returns.NotFound("mapset");

        return set.GetVotes(auth?.ID ?? 0);
    }

    [Authenticated]
    [HttpRoute("/:id/votes", APIMethod.Post)]
    public APIReturn<APIMapVotes> UpdateVoteState(User auth, long id, [Source(ParameterSource.Form)] int vote)
    {
        var set = maps.GetSet(id);
        if (set is null) return Returns.NotFound("mapset");

        vote = Math.Clamp(vote, -1, 1);
        set.SetVote(auth.ID, vote);
        maps.Update(set);

        return set.GetVotes(auth.ID);
    }

    #endregion

    private void notifyAction(ModdingAction action) => tasks.Schedule(new MethodTask(() => events.QueueActionCreate(action.ID)));
}
