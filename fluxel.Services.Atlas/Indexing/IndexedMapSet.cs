using System;
using System.Text.Json.Serialization;

namespace fluxel.Services.Atlas.Indexing;

public class IndexedMapSet
{
    [JsonPropertyName("id")]
    public long ID { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("artist")]
    public string Artist { get; set; } = string.Empty;

    [JsonPropertyName("title_romanized")]
    public string TitleRomanized { get; set; } = string.Empty;

    [JsonPropertyName("artist_romanized")]
    public string ArtistRomanized { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("modes")]
    public string[] Modes { get; set; } = [];

    [JsonPropertyName("tags")]
    public string[] Tags { get; set; } = [];

    [JsonPropertyName("sources")]
    public string[] Sources { get; set; } = [];

    [JsonPropertyName("created")]
    public DateTime DateCreated { get; set; }

    [JsonPropertyName("ranked")]
    public DateTime? DateRanked { get; set; }
}
