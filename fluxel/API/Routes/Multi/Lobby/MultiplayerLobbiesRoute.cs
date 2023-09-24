using System.Net;
using fluxel.API.Components;
using fluxel.Multiplayer.OpenLobby;

namespace fluxel.API.Routes.Multi.Lobby;

public class MultiplayerLobbiesRoute : IApiRoute {
    public string Path => "/multi/lobbies";
    public string Method => "GET";

    public ApiResponse Handle(HttpListenerRequest req, HttpListenerResponse res, Dictionary<string, string> parameters) {
        return new ApiResponse {
            Data = LobbyHandler.Lobbies
        };
    }
}
