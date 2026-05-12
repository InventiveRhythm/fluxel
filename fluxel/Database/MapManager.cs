using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using fluxel.Config;
using fluxel.Database.Extensions;
using fluxel.Models;
using fluxel.Models.Maps;
using fluxel.Models.Maps.Modding;
using fluXis.Online.API.Models.Maps.Modding;
using Midori.Database;
using MongoDB.Bson;

namespace fluxel.Database;

public class MapManager
{
    public const long MAX_MAPSETS_IN_QUEUE = 2;
    public const int MAX_PACKAGE_SIZE = 75 * 1024 * 1024;
    public const int REQUIRED_VOTES = 2;

    public const string MAP_TABLE_NAME = "maps";
    public const string MAPSET_TABLE_NAME = "mapsets";

    private readonly IDatabaseTable<Map> maps;
    private readonly IDatabaseTable<MapSet> sets;
    private readonly IDatabaseTable<MapRateVote> votes;
    private readonly IDatabaseTable<MapFavorite> favorite;
    private readonly IDatabaseTable<ModdingAction> actions;

    private readonly CounterManager counters;
    private readonly ServerConfig config;

    public MapManager(IDatabaseProvider db, CounterManager counters, ServerConfig config)
    {
        maps = db.GetTable<Map>(MAP_TABLE_NAME);
        sets = db.GetTable<MapSet>(MAPSET_TABLE_NAME);
        votes = db.GetTable<MapRateVote>("rate-votes");
        favorite = db.GetTable<MapFavorite>("mapsets-love");
        actions = db.GetTable<ModdingAction>("modding-actions");

        this.counters = counters;
        this.config = config;
    }

    #region Maps

    public List<Map> AllMaps => maps.Find(_ => true).ToList();
    public List<Map> NeedRefresh => maps.Find(x => x.NeedsScoreRefresh).ToList();
    public long PureMapCount => AllPureSets.Sum(x => x.Maps.Count());

    public void Add(Map map)
    {
        map.ID = counters.GetNext(CounterType.Map);
        maps.Add(map);
    }

    public void Update(Map map) => maps.Replace(m => m.ID == map.ID, map);
    public void RemoveMap(Map map) => maps.Delete(m => m.ID == map.ID);

    public Map? GetMap(long id) => maps.Find(m => m.ID == id).FirstOrDefault();
    public Map? GetMap(Expression<Func<Map, bool>> filter) => maps.Find(filter).FirstOrDefault();
    public Map? GetMapByHash(string hash) => maps.Find(m => m.SHA256Hash == hash).FirstOrDefault();
    public List<Map> GetByMapsByMapper(long id) => maps.Find(x => x.MapperID == id).ToList();

    public bool TryGetMap(long id, [NotNullWhen(true)] out Map? map)
    {
        map = GetMap(id);
        return map != null;
    }

    public void QuickUpdate(long id, Action<Map>? action)
    {
        var u = GetMap(id);

        if (u is null)
            throw new ArgumentNullException(nameof(id), "No map with the provided ID found.");

        action?.Invoke(u);
        Update(u);
    }

    public void RemoveMapsBySet(long id) => maps.DeleteMultiple(x => x.SetID == id);

    #endregion

    #region MapSets

    public List<MapSet> AllSets => sets.Find(_ => true).ToList();
    public List<MapSet> AllQueueSets => sets.Find(m => m.Status >= MapStatus.Pure).ToList();
    public List<MapSet> AllPureSets => sets.Find(x => x.AllowScores()).ToList();

    public long SetCount => sets.Count(_ => true);
    public long PureSetCount => sets.Count(x => x.AllowScores());

    public void Add(MapSet set)
    {
        set.ID = counters.GetNext(CounterType.MapSet);
        sets.Add(set);
    }

    public void Update(MapSet set) => sets.Replace(m => m.ID == set.ID, set);

    public void RemoveSet(long id)
    {
        sets.Delete(m => m.ID == id);
        RemoveMapsBySet(id);
    }

    public MapSet? GetSet(long id) => sets.Find(x => x.ID == id).FirstOrDefault();
    public List<MapSet> GetSetsByCreator(long id) => sets.Find(m => m.CreatorID == id).ToList();

    #endregion

    #region Voting

    public void AddRateVote(long user, long map, float rating, float read, float track, float percept, bool purifier)
    {
        var vote = new MapRateVote
        {
            UserID = user,
            MapID = map,
            BaseRating = (float)Math.Round(Math.Clamp(rating, 0, 20), 1),
            ReadingRating = (float)Math.Round(Math.Clamp(read, 0, 5), 1),
            TrackingRating = (float)Math.Round(Math.Clamp(track, 0, 5), 1),
            PerceptionRating = (float)Math.Round(Math.Clamp(percept, 0, 5), 1),
            PurifierVote = purifier
        };

        votes.Add(vote);
    }

    public void ClearRateVotes(long map) => votes.DeleteMultiple(x => x.MapID == map);
    public bool HasRateVoted(long user, long map) => votes.Find(m => m.UserID == user && m.MapID == map).Any();
    public List<MapRateVote> GetRateVotesByMap(long map) => votes.Find(m => m.MapID == map).ToList();

    #endregion

    #region Limits

    public DateTimeOffset UploadLimitStartDate => new(new DateTime(2026, 1, 1));

    public long CountUploaded(long id, DateTimeOffset? after = null)
        => sets.Count(x => x.CreatorID == id && x.Submitted >= (after ?? DateTimeOffset.MinValue));

    public long CountInQueue(long id)
        => sets.Count(m => m.CreatorID == id && m.Status == MapStatus.Pending);

    public long GetUploadLimit(long id)
    {
        var count = config.Limits.MaxMapSets;
        var inc = config.Limits.IncreasePerPure;

        var pure = sets.Count(x => x.CreatorID == id && x.Status >= MapStatus.Pure);
        return count + inc * pure;
    }

    #endregion

    #region Modding

    public bool HasActions(long set) => actions.Count(x => x.MapSetID == set) > 0;
    public ModdingAction? GetAction(ObjectId id) => actions.Find(x => x.ID == id).FirstOrDefault();

    public ModdingAction CreateModAction(long set, long user, APIModdingActionType type, string? content = null)
    {
        var action = new ModdingAction
        {
            ID = ObjectId.GenerateNewId(),
            MapSetID = set,
            UserID = user,
            Type = type,
            Content = content,
            Time = DateTimeOffset.Now.ToUnixTimeSeconds()
        };

        actions.Add(action);

        return action;
    }

    public List<ModdingAction> GetModActionsFromSet(long id)
    {
        var mods = actions.Find(x => x.MapSetID == id).ToList();
        mods.Sort((a, b) => -a.Time.CompareTo(b.Time));
        return mods;
    }

    #endregion

    #region Favorite

    public bool HasFavorite(long user, long set)
        => favorite.Find(x => x.UserID == user && x.MapSetID == set).FirstOrDefault() != null;

    public List<long> AllFavoriteByUser(long user)
        => favorite.Find(x => x.UserID == user).ToList().Select(x => x.MapSetID).ToList();

    public List<long> AllFavoriteBySet(long set)
        => favorite.Find(x => x.MapSetID == set).ToList().Select(x => x.UserID).ToList();

    public void AddFavorite(long user, long set)
    {
        if (HasFavorite(user, set))
            return;

        favorite.Add(new MapFavorite { MapSetID = set, UserID = user });
    }

    public void RemoveFavorite(long user, long set) => favorite.Delete(x => x.UserID == user && x.MapSetID == set);

    #endregion
}
