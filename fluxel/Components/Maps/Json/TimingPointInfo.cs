using JetBrains.Annotations;

namespace fluxel.Components.Maps.Json;

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
public class TimingPointInfo {
    public float Time { get; init; }
    public float BPM { get; init; }
    public int Signature { get; init; }
}
