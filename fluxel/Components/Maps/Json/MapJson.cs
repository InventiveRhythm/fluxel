namespace fluxel.Components.Maps.Json;

// ReSharper disable CollectionNeverUpdated.Global
public class MapJson {
    public string AudioFile { get; init; } = string.Empty;
    public string BackgroundFile { get; init; } = string.Empty;
    public string CoverFile { get; set; } = string.Empty;
    public string VideoFile { get; init; } = string.Empty;
    public string EffectFile { get; init; } = string.Empty;
    public MapMetadataJson Metadata { get; init; } = new();
    public List<HitObjectInfo> HitObjects { get; init; } = new();
    public List<TimingPointInfo> TimingPoints { get; init; } = new();
    public List<ScrollVelocityInfo> ScrollVelocities { get; init; } = new();

    public int KeyCount => HitObjects.Count > 0 ? HitObjects.Max(x => x.Lane) : 0;

    public bool Validate() {
        if (HitObjects.Count == 0)
            return false;

        if (TimingPoints.Count == 0)
            return false;

        foreach (var timingPoint in TimingPoints)
        {
            if (timingPoint.BPM <= 0)
                return false;

            if (timingPoint.Signature <= 0)
                return false;
        }

        return KeyCount is >= 4 and <= 8;
    }
}
