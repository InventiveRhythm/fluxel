using fluxel.Components;
using fluxel.Modules;
using fluxel.Modules.Messages;
using fluxel.Multiplayer.Lobby;
using fluXis.Online.API.Models.Multi;
using Microsoft.Extensions.DependencyInjection;
using Midori.Networking;

namespace fluxel.Multiplayer;

public class MultiplayerModule : IModule, IMultiRoomManager
{
    public static HttpConnectionManager<MultiplayerSocket> Sockets { get; private set; } = null!;

    private readonly IServiceProvider services;

    public MultiplayerModule(HttpRouter router, IServiceProvider services)
    {
        this.services = services;
        Sockets = router.MapModule<MultiplayerSocket>("/multiplayer", manager: true)!;
        MultiplayerRoomManager.StartThread();
    }

    public void OnMessage(object data)
    {
        switch (data)
        {
            case UserOnlineStateMessage onl:
            {
                if (onl.Online) return;

                var sock = Sockets.FirstOrDefault(x => x.UserID == onl.UserID);
                sock?.LeaveRoom();
                break;
            }
        }
    }

    MultiplayerRoom? IMultiRoomManager.WithPlayer(long id) => MultiplayerRoomManager.GetCurrentRoom(id)?.ToAPI(services.CreateScope().ServiceProvider.GetRequiredService<ModelTranslator>());
}
