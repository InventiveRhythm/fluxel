using System;
using System.Collections.Generic;
using fluxel.Database;
using fluXis.Online.API.Models.Maps;
using fluXis.Utils;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace fluxel.Models.Maps;

[JsonObject(MemberSerialization.OptIn)]
public class Map : IHasID
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

    [BsonElement("hits")]
    public int Hits { get; set; }

    [BsonElement("lns")]
    public int LongNotes { get; set; }

    [BsonElement("ticks")]
    public int TickNotes { get; set; }

    [BsonElement("mines")]
    public int Landmines { get; set; }

    [BsonElement("effects")]
    public MapEffectType Effects { get; set; }

    [BsonElement("nps")]
    public double NotesPerSecond { get; set; }

    [BsonElement("votes")]
    public Dictionary<string, int>? Votes { get; set; } = new();

    [BsonElement("needs-score-refresh")]
    public bool NeedsScoreRefresh { get; set; } = true;

    [BsonIgnore]
    public int MaxCombo => Hits + LongNotes * 2 + TickNotes + Landmines;

    [BsonIgnore]
    public string SortingTitle => string.IsNullOrEmpty(TitleRomanized) ? Title : TitleRomanized;

    [BsonIgnore]
    public string SortingArtist => string.IsNullOrEmpty(ArtistRomanized) ? Artist : ArtistRomanized;

    [BsonIgnore]
    public string Metadata => $"{Title} - {Artist} [{DifficultyName}]";

    public double RecalculateRating(MapManager maps)
    {
        var r = Rating;

        var votes = maps.GetRateVotesByMap(ID);

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
        Rating = baseRate + effectRate * 2;

        if (Math.Abs(r - Rating) > 0.001f)
            NeedsScoreRefresh = true;

        return Rating;
    }
}

[Flags]
public enum MapIncludes
{
    FileName = 1 << 0,
    Claims = 1 << 1
}
