using fluxel.Components;
using fluxel.Multiplayer.Lobby;
using fluXis.Online.API.Models.Multi;
using Midori.API.Attributes;
using Midori.API.Components;

namespace fluxel.Multiplayer;

[Controller("/multi")]
public class MultiplayerController
{
    private readonly ModelTranslator translator;

    public MultiplayerController(ModelTranslator translator)
    {
        this.translator = translator;
    }

    [Authenticated]
    [HttpRoute("/lobbies")]
    public APIReturn<List<MultiplayerRoom>> List()
        => MultiplayerRoomManager.Lobbies.Select(x => x.ToAPI(translator)).ToList();
}
