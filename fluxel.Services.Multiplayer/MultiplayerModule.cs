/*
using fluxel.Components;
using fluxel.Services.Multiplayer.Lobby;
using fluXis.Online.API.Models.Multi;
using Midori.Networking;

namespace fluxel.Services.Multiplayer;

public class MultiplayerModule : IModule, IMultiRoomManager
{
    public void OnMessage(IServiceProvider _, object data)
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
*/
