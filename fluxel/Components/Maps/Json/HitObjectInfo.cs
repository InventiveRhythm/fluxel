using JetBrains.Annotations;

namespace fluxel.Components.Maps.Json;

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
public class HitObjectInfo {
    public float Time { get; init; }
    public int Lane { get; init; }
    public float HoldTime { get; init; }
}
