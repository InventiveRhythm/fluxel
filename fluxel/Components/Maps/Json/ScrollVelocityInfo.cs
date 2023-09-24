using JetBrains.Annotations;

namespace fluxel.Components.Maps.Json;

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
public class ScrollVelocityInfo {
    public float Time { get; init; }
    public float Multiplier { get; init; }
}
