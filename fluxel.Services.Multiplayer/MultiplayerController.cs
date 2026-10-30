using System.Linq;
using fluxel.Components;
using fluxel.Models;
using fluxel.Services.Multiplayer.Lobby;
using fluXis.Online.API.Models.Multi;
using Midori.API.Attributes;
using Midori.API.Components;

namespace fluxel.Services.Multiplayer;

[Controller("/multiplayer")]
public class MultiplayerController
{
    private readonly DatabaseContext database;
    private readonly ModelTranslator translator;

    public MultiplayerController(ModelTranslator translator, DatabaseContext database)
    {
        this.translator = translator;
        this.database = database;
    }

    [Authenticated]
    [HttpRoute("/lobbies")]
    public APIReturn<MultiplayerRoom[]> List()
        => MultiplayerRoomManager.Lobbies.Select(x => x.ToAPI(database, translator)).ToArray();
}
