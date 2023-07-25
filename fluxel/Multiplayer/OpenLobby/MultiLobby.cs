using System.Net;
using fluxel.Components.Maps;
using fluxel.Components.Users;
using fluxel.Websocket;
using Newtonsoft.Json;

namespace fluxel.Multiplayer.OpenLobby;

public class MultiLobby {
    [JsonProperty("id")]
    public int RoomId { get; set; }

    [JsonProperty("settings")]
    public MultiLobbySettings Settings { get; set; } = new();

    [JsonIgnore]
    public int HostId { get; init; }

    [JsonProperty("host")]
    public UserShort Host => User.FindById(HostId)?.ToShort() ?? new UserShort();

    [JsonIgnore]
    public Dictionary<IPEndPoint, int> Users { get; set; } = new();

    [JsonProperty("users")]
    public List<UserShort> UsersShort => Users.Values.Select(id => User.FindById(id)?.ToShort() ?? new UserShort()).ToList();

    [JsonIgnore]
    public List<int> Maps { get; init; } = new();

    [JsonProperty("maps")]
    public List<MapShort> MapList => Maps.Select(id => (Map.FindById(id) ?? new Map()).ToShort()).ToList();

    private void sendToAll(string message) {
        foreach (var user in Users.Keys) {
            if (WebsocketConnection.CONNECTIONS.TryGetValue(user, out var connection))
                connection.Send(message);
        }
    }

    public void SendToAll(object message) => sendToAll(JsonConvert.SerializeObject(message));
}

public class MultiLobbySettings {
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonIgnore]
    public string Password { get; set; } = string.Empty;

    [JsonProperty("password")]
    public bool HasPassword => !string.IsNullOrEmpty(Password);
}
