using System.Linq;
using fluxel.Components;
using fluxel.Services.Multiplayer.Lobby;
using fluXis.Online.API.Models.Multi;
using Midori.API.Attributes;
using Midori.API.Components;

namespace fluxel.Services.Multiplayer;

[Controller("/multiplayer")]
public class MultiplayerController
{
    private readonly ModelTranslator translator;

    public MultiplayerController(ModelTranslator translator)
    {
        this.translator = translator;
    }

    [Authenticated]
    [HttpRoute("/lobbies")]
    public APIReturn<MultiplayerRoom[]> List()
        => MultiplayerRoomManager.Lobbies.Select(x => x.ToAPI(translator)).ToArray();
}
