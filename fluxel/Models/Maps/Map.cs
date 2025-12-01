using System;
using System.Collections.Generic;
using fluxel.API.Components;
using fluxel.Database.Helpers;
using fluXis.Online.API.Models.Maps;
using fluXis.Utils;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace fluxel.Models.Maps;

[JsonObject(MemberSerialization.OptIn)]
public class Map : IHasCache
{
    [BsonId]
    public long ID { get; set; }

    [BsonElement("set")]
    public long SetID { get; set; }

    [BsonElement("file")]
    public string FileName { get; set; } = "";

    [BsonElement("hash")]
    public string SHA256Hash { get; set; } = "";

    [BsonElement("effect-hash")]
    public string EffectSHA256Hash { get; set; } = "";

    [BsonElement("sb-hash")]
    public string StoryboardSHA256Hash { get; set; } = "";

    [BsonIgnore]
    public string FullHash => MapUtils.GetHash($"{SHA256Hash}{EffectSHA256Hash}{StoryboardSHA256Hash}");

    [BsonElement("mapper")]
    public long MapperID { get; set; }

    [BsonElement("difficulty")]
    public string DifficultyName { get; set; } = "";

    [BsonElement("accuracy")]
    public float AccuracyDifficulty { get; set; }

    [BsonElement("health")]
    public float HealthDifficulty { get; set; }

    [BsonElement("mode")]
    public int Mode { get; set; }

    [BsonElement("title")]
    public string Title { get; set; } = "";

    [BsonElement("title-rm")]
    public string TitleRomanized { get; set; } = "";

    [BsonElement("artist")]
    public string Artist { get; set; } = "";

    [BsonElement("artist-rm")]
    public string ArtistRomanized { get; set; } = "";

    [BsonElement("source")]
    public string Source { get; set; } = "";

    [BsonElement("tags")]
    public string Tags { get; set; } = "";

    [BsonElement("bpm")]
    public double BPM { get; set; }

    [BsonElement("length")]
    public int Length { get; set; }

    [BsonElement("rating")]
    public double Rating { get; set; }

    //these are the hits only for the main player. For dual maps, see "DualSides"
    [BsonElement("hits")]
    public int Hits { get; set; }

    //these are the lns only for the main player. For dual maps, see "DualSides"
    [BsonElement("lns")]
    public int LongNotes { get; set; }

    [BsonElement("dual-sides")]
    public List<MapDualSide> DualSides { get; set; } = new();

    [BsonElement("effects")]
    public MapEffectType Effects { get; set; }

    [BsonElement("nps")]
    public double NotesPerSecond { get; set; }

    [BsonElement("votes")]
    public Dictionary<string, int>? Votes { get; set; } = new();

    [BsonElement("player-count")]
    public int PlayerCount { get; set; } = 1;

    [BsonIgnore]
    public int MaxCombo => Hits + LongNotes * 2;

    [BsonIgnore]
    public RequestCache Cache { get; set; } = new();

    [BsonIgnore]
    public MapSet? MapSet => Cache.MapSets.Get(SetID);

    [BsonIgnore]
    public string SortingTitle => string.IsNullOrEmpty(TitleRomanized) ? Title : TitleRomanized;

    [BsonIgnore]
    public string SortingArtist => string.IsNullOrEmpty(ArtistRomanized) ? Artist : ArtistRomanized;

    [BsonIgnore]
    public string Metadata => $"{Title} - {Artist} [{DifficultyName}]";

    [BsonIgnore]
    public string Url => ServerHost.Configuration.Urls.Website + $"/mapset/{SetID}";

    [BsonIgnore]
    public string BackgroundUrl => ServerHost.Configuration.Urls.Assets + $"/background/{SetID}-lg";

    [BsonIgnore]
    public string CoverUrl => ServerHost.Configuration.Urls.Assets + $"/cover/{SetID}-lg";

    public double RecalculateRating()
    {
        var votes = MapHelper.GetVotesByMap(ID);

        if (votes.Count == 0)
            return Rating = 0;

        var count = 0;

        var baseTotal = 0d;
        var readTotal = 0d;
        var trackTotal = 0d;
        var perceptTotal = 0d;

        foreach (var vote in votes)
        {
            var factor = vote.PurifierVote ? 25 : 1;

            baseTotal += vote.BaseRating * factor;
            readTotal += vote.ReadingRating * factor;
            trackTotal += vote.TrackingRating * factor;
            perceptTotal += vote.PerceptionRating * factor;
            count += factor;
        }

        var baseRate = baseTotal / count;
        var effectRate = (readTotal + trackTotal + perceptTotal) / 3 / count;
        return Rating = baseRate + effectRate * 2;
    }

    public int MaxComboForPlayer(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= PlayerCount) return 0;
        if (playerIndex == 0) return MaxCombo;

        int dualIndex = playerIndex - 1;
        if (dualIndex >= DualSides.Count) return 0;

        return DualSides[dualIndex].MaxCombo;
    }

    public class MapDualSide
    {
        [BsonElement("hits")]
        public int Hits { get; set; }

        [BsonElement("lns")]
        public int LongNotes { get; set; }

        [BsonIgnore]
        public int MaxCombo => Hits + LongNotes * 2;
    }
}

[Flags]
public enum MapIncludes
{
    FileName = 1 << 0,
    Claims = 1 << 1
}
