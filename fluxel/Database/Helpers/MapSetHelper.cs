using System;
using System.Collections.Generic;
using System.Linq;
using fluxel.Models;
using fluxel.Models.Maps;
using fluxel.Models.Maps.Modding;
using fluxel.Tasks.Other;
using fluXis.Online.API.Models.Maps.Modding;
using MongoDB.Bson;
using MongoDB.Driver;

namespace fluxel.Database.Helpers;

public static class MapSetHelper
{
    public const long MAX_MAPSETS_IN_QUEUE = 2;
    public const int MAX_PACKAGE_SIZE = 75 * 1024 * 1024;
    public const int REQUIRED_VOTES = 2;

    private static IMongoCollection<MapSet> sets => MongoDatabase.GetCollection<MapSet>("mapsets");

    public static List<MapSet> All => sets.Find(m => true).ToList();
    public static List<MapSet> AllPure => sets.Find(m => m.Status >= MapStatus.Pure).ToList();
    public static List<MapSet> AllInQueue => sets.Find(m => m.Status == MapStatus.Pending).ToList();

    public static long Count => sets.CountDocuments(m => true);
    public static long InQueueByCount(long uid) => sets.CountDocuments(m => m.CreatorID == uid && m.Status == MapStatus.Pending);

    public static void Add(MapSet set)
    {
        set.ID = CounterHelper.GetNext(CounterType.MapSet);
        sets.InsertOne(set);
    }

    public static void Update(MapSet set) => sets.ReplaceOne(m => m.ID == set.ID, set);

    public static MapSet? Get(long id) => sets.Find(m => m.ID == id).FirstOrDefault();
    public static IEnumerable<MapSet> GetByCreator(long id) => sets.Find(m => m.CreatorID == id).ToList();

    public static void Delete(long id)
    {
        sets.DeleteOne(m => m.ID == id);
        MapHelper.DeleteBySet(id);
    }

    public static long GetUploadLimit(long uid)
    {
        var count = ServerHost.Configuration.Limits.MaxMapSets;
        var inc = ServerHost.Configuration.Limits.IncreasePerPure;

        var pure = sets.CountDocuments(x => x.CreatorID == uid && x.Status >= MapStatus.Pure);
        return count + inc * pure;
    }

    public static DateTimeOffset UploadLimitStartDate => new(new DateTime(2026, 1, 1));

    public static long GetUploadedCount(long uid, DateTimeOffset? after = null)
        => sets.CountDocuments(x => x.CreatorID == uid && x.Submitted >= (after ?? DateTimeOffset.MinValue));

    #region Modding

    private static IMongoCollection<ModdingAction> actions => MongoDatabase.GetCollection<ModdingAction>("modding-actions");

    public static bool HasActions(long set) => actions.CountDocuments(x => x.MapSetID == set) > 0;
    public static ModdingAction? GetAction(ObjectId id) => actions.Find(x => x.ID == id).FirstOrDefault();

    public static ModdingAction CreateModAction(long set, long user, APIModdingActionType type, string? content = null)
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

        actions.InsertOne(action);
        ServerHost.Instance.Scheduler.Schedule(new MethodTask(() => Events.QueueActionCreate(action.ID)));

        return action;
    }

    public static List<ModdingAction> GetModActionsFromSet(long id)
    {
        var mods = actions.Find(x => x.MapSetID == id).ToList();
        mods.Sort((a, b) => -a.Time.CompareTo(b.Time));
        return mods;
    }

    #endregion

    #region Favorite

    private static IMongoCollection<MapFavorite> favorite => MongoDatabase.GetCollection<MapFavorite>("mapsets-love");

    public static bool HasFavorite(long user, long set)
        => favorite.Find(x => x.UserID == user && x.MapSetID == set).FirstOrDefault() != null;

    public static List<long> AllFavoriteByUser(long user)
        => favorite.Find(x => x.UserID == user).ToList().Select(x => x.MapSetID).ToList();

    public static List<long> AllFavoriteBySet(long set)
        => favorite.Find(x => x.MapSetID == set).ToList().Select(x => x.UserID).ToList();

    public static void AddFavorite(long user, long set)
    {
        if (HasFavorite(user, set))
            return;

        favorite.InsertOne(new MapFavorite { MapSetID = set, UserID = user });
    }

    public static void RemoveFavorite(long user, long set) => favorite.DeleteOne(x => x.UserID == user && x.MapSetID == set);

    #endregion
}
