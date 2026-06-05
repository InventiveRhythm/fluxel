using fluxel.Modules;
using Midori.Networking;

namespace fluxel.Spectator;

public class SpectatorModule : IModule
{
    public HttpConnectionManager<SpectatorSocket> Sockets { get; private set; }

    public SpectatorModule(HttpRouter router)
    {
        Sockets = router.MapModule<SpectatorSocket>("/spectator", manager: true)!;
    }
}
