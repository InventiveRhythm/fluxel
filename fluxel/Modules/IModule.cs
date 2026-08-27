using System;
using JetBrains.Annotations;

namespace fluxel.Modules;

[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public interface IModule
{
    void OnMessage(IServiceProvider services, object data) { }
}
