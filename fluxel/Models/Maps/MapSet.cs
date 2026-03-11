using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using fluxel.API.Components;
using fluxel.Database.Helpers;
using fluxel.Models.Maps.Modding;
using fluXis.Online.API.Models.Maps;
using fluXis.Online.API.Models.Maps.Modding;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace fluxel.Models.Maps;

[JsonObject(MemberSerialization.OptIn)]
public class MapSet : IHasCache
{
    [BsonId]
    public long ID { get; set; }

    [BsonElement("creator")]
    public long CreatorID { get; init; }

    [BsonElement("title")]
    public string Title { get; set; } = "";

    [BsonElement("title-rm")]
    public string TitleRomanized { get; set; } = "";

    [BsonElement("artist")]
    public string Artist { get; set; } = "";

    [BsonElement("artist-rm")]
    public string ArtistRomanized { get; set; } = "";

    [BsonElement("status")]
    public MapStatus Status { get; set; }

    [BsonElement("maps")]
    public IEnumerable<long> Maps { get; set; } = new List<long>();

    /// <summary>
    /// the time the mapset was submitted to the queue
    /// </summary>
    [BsonElement("queue-time")]
    public long? QueueTime { get; set; }

    [BsonElement("queue-votes")]
    public List<ModQueueVote> QueueVotes { get; set; } = new();

    [BsonIgnore]
    public string SortingTitle => string.IsNullOrEmpty(TitleRomanized) ? Title : TitleRomanized;

    [BsonIgnore]
    public string SortingArtist => string.IsNullOrEmpty(ArtistRomanized) ? Artist : ArtistRomanized;

    [BsonIgnore]
    public List<Map> MapsList
    {
        get
        {
            var maps = Maps.Select(Cache.Maps.Get).OfType<Map>().ToList();
            return maps;
        }
    }

    [BsonElement("submitted")]
    public DateTimeOffset Submitted { get; set; } = DateTimeOffset.UtcNow;

    [BsonElement("updated")]
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;

    [BsonElement("ranked")]
    public DateTimeOffset? DateRanked { get; set; }

    [BsonElement("flags")]
    public MapSetFlag Flags { get; set; }

    [BsonElement("internal-flags")]
    public InternalSetFlags InternalFlags { get; set; }

    [BsonElement("description")]
    public string Description { get; set; } = "";

    [BsonElement("votes")]
    public Dictionary<string, int> Votes { get; set; } = new();

    [BsonIgnore]
    public long UpVotes => Votes.Count(x => x.Value == 1);

    [BsonIgnore]
    public long DownVotes => Votes.Count(x => x.Value == -1);

    [BsonIgnore]
    public string[] Tags
    {
        get
        {
            var tags = new List<string>();

            MapsList.ForEach(map =>
            {
                var split = map.Tags.Split(',');

                foreach (var s in split)
                {
                    var trim = s.Trim();

                    if (!tags.Contains(trim))
                        tags.Add(trim);
                }
            });

            return tags.ToArray();
        }
    }

    [BsonIgnore]
    public string Source
    {
        get
        {
            Dictionary<string, int> sources = new();

            MapsList.ForEach(map =>
            {
                if (!sources.TryAdd(map.Source, 1))
                    sources[map.Source]++;
            });

            return sources.Count == 0 ? "" : sources.MaxBy(pair => pair.Value).Key;
        }
    }

    [BsonIgnore]
    public RequestCache Cache { get; set; } = new();

    [BsonIgnore]
    public string Url => ServerHost.Configuration.Urls.Website.Replace("fluxis.", "dev.") + $"/set/{ID}";

    [BsonIgnore]
    public string BackgroundUrl => ServerHost.Configuration.Urls.Assets + $"/background/{ID}-lg";

    [BsonIgnore]
    public string CoverUrl => ServerHost.Configuration.Urls.Assets + $"/cover/{ID}-lg";

    public bool AddModdingEntry(APIModdingActionType type, long user, out string error)
    {
        error = "";

        var isVote = type is APIModdingActionType.Approve or APIModdingActionType.Deny;

        if (!isVote)
            return true;

        if (QueueVotes.Any(x => x.UserID == user))
        {
            error = "You have already voted for this map.";
            return false;
        }

        var approve = type == APIModdingActionType.Approve;

        if (approve && Maps.Any(map => !MapHelper.HasVoted(user, map)))
        {
            error = "You have not submitted a rate vote for all maps.";
            return false;
        }

        QueueVotes.Add(new ModQueueVote(user, approve));

        if (approve)
        {
            if (QueueVotes.Count(x => x.Approve) >= MapSetHelper.REQUIRED_VOTES)
            {
                Status = MapStatus.Pure;
                DateRanked = DateTimeOffset.UtcNow;

                foreach (var map in Maps)
                    ScoreHelper.DeleteAllFromMap(map);
            }
        }
        else
            Status = MapStatus.Impure;

        MapSetHelper.Update(this);
        return true;
    }
}

public enum MapStatus
{
    /// <summary>
    /// fully blocked from being ranked
    /// </summary>
    Blacklisted = -1,

    /// <summary>
    /// map has not been submitted to the queue
    /// </summary>
    Unsubmitted = 0,

    /// <summary>
    /// this map is in the queue
    /// </summary>
    Pending = 1,

    /// <summary>
    /// has been denied from being ranked but can still re-submit
    /// </summary>
    Impure = 2,

    /// <summary>
    /// map is ranked and can be scored on
    /// </summary>
    Pure = 3,

    /// <summary>
    /// map can show up in ranked matches. this shows up as "PURE" with a gold background
    /// </summary>
    Ranked = 4
}

[Flags]
public enum InternalSetFlags : long
{
    [Description("Shadow-Ban (hide from search results)")]
    ShadowBan = 1 << 0
}

[Flags]
public enum MapSetInclude
{
    QueueInfo = 1 << 0
}
