using fluxel.Database.Extensions;
using fluxel.Database.Helpers;
using fluxel.Multiplayer.Lobby;
using fluxel.Utils;
using fluxel.WebSocket;
using fluXis.Online.API.Models.Multi;
using fluXis.Online.Exceptions;
using fluXis.Online.Multiplayer;
using fluXis.Scoring;
using fluXis.Scoring.Structs;

namespace fluxel.Multiplayer;

public class MultiplayerSocket : AuthenticatedSocket<IMultiplayerServer, IMultiplayerClient>, IMultiplayerServer
{
    public ServerMultiplayerRoom? Room { get; set; }

    public IEnumerable<MultiplayerSocket> All => Room is null ? [] : Room.All;

    public Task<MultiplayerRoom> CreateRoom(string name, MultiplayerPrivacy privacy, string password, long map, string hash)
    {
        if (Room is not null)
            throw RoomException.AlreadyInRoom();

        Room = MultiplayerRoomManager.Create(this, name, privacy, password, map);
        return Task.FromResult(Room.ToAPI());
    }

    public async Task<MultiplayerRoom> JoinRoom(long id, string password)
    {
        if (Room is not null)
            throw RoomException.AlreadyInRoom();

        var room = MultiplayerRoomManager.FindRoom(id);

        if (room is null)
            throw RoomException.RoomNotFound();

        switch (room.Privacy)
        {
            case MultiplayerPrivacy.Club:
                break;

            case MultiplayerPrivacy.Private:
                if (room.Password != password)
                    throw new InvalidRoomPasswordException();

                break;
        }

        if (!UserHelper.TryGet(UserID, out _))
            return null!;

        var participant = new ServerMultiplayerRoom.Participant(this);
        room.Participants.Add(participant);
        Room = room;

        await All.ForEachAsync(c => c.Client.UserJoined(participant.ToAPI()));
        return Room.ToAPI();
    }

    public Task KickPlayer(long id)
    {
        if (Room is null)
            throw RoomException.NotInRoom();

        return Task.CompletedTask;
    }

    public async Task LeaveRoom()
    {
        var user = Room?.Participants.FirstOrDefault(u => u.ID == UserID);

        if (Room == null || user == null)
            return;

        await All.ForEachAsync(c => c.Client.UserLeft(user.ID));
        Room.Participants.Remove(user);

        // who knows maybe they leave in the middle of a game
        // while all others are done
        await endIfAllFinished();

        // remove room if no players are left
        if (Room.Participants.Count != 0)
        {
            if (Room.HostID == UserID)
            {
                var first = Room.Participants.First();
                await TransferHost(first.ID);
            }
        }
        else
            MultiplayerRoomManager.Remove(Room);

        Room = null;
    }

    public async Task UpdateReadyState(bool ready)
    {
        if (Room is null)
            throw RoomException.NotInRoom();

        var player = Room.Participants.FirstOrDefault(u => u.ID == UserID);

        if (player == null)
            return;

        switch (ready)
        {
            case true when player.State == MultiplayerUserState.Idle:
                setPlayerStatus(UserID, MultiplayerUserState.Ready);
                break;

            case false:
                setPlayerStatus(UserID, MultiplayerUserState.Idle);
                break;
        }

        await startIfAllReady();
    }

    public async Task TransferHost(long id)
    {
        if (Room is null)
            throw RoomException.NotInRoom();

        if (Room.HostID != UserID || Room.HostID == id)
            return;

        Room.HostID = id;
        await All.ForEachAsync(c => c.Client.HostChanged(Room.HostID));
    }

    public async Task<bool> UpdateMap(long map, string hash, List<string> mods)
    {
        if (Room is null) throw RoomException.NotInRoom();
        if (Room.HostID != UserID) throw RoomException.NotHost();
        if (!MapHelper.TryGetMap(map, out var m)) throw MultiMapException.NotFound();
        if (m.SHA256Hash != hash) throw MultiMapException.Mismatch();

        Room.MapID = map;
        Room.CurrentMods = mods.ToList();
        await All.ForEachAsync(c => c.Client.MapUpdated(m.ToAPI(), Room.CurrentMods));

        foreach (var user in Room.Participants.Where(u => u.State < MultiplayerUserState.Playing))
            setPlayerStatus(user.ID, MultiplayerUserState.Idle);

        return true;
    }

    public Task UpdateScore(int score)
    {
        if (Room is null)
            throw RoomException.NotInRoom();

        Room.RunLocked(() => Room.ScheduledScores.Add((UserID, score)));
        return Task.CompletedTask;
    }

    public async Task FinishPlay(ScoreInfo score)
    {
        if (Room is null)
            throw RoomException.NotInRoom();

        var player = Room.GetPlayer(UserID);

        if (player == null)
            return;

        //TODO: adapt for dual
        score.Players[0].PlayerID = UserID;
        setPlayerStatus(player.ID, MultiplayerUserState.Finished);

        score.Players[0].HitResults = new List<HitResult>();
        player.Score = score;

        await endIfAllFinished();
    }

    protected override void OnClose()
    {
        base.OnClose();
        _ = LeaveRoom();
    }

    private async Task startIfAllReady()
    {
        var participants = Room?.Participants;

        if (Room is null || participants is null)
            return;

        if (participants.Any(u => u.State != MultiplayerUserState.Ready))
            return;

        var time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        time += 5000;

        Room.RunLocked(() => Room.CountdownFinishTime = time);
        await All.ForEachAsync(c => c.Client.CountdownStarted(time));
    }

    private async Task endIfAllFinished()
    {
        if (Room is null)
            return;

        var participants = Room.Participants;

        if (participants.Any(u => u.State == MultiplayerUserState.Playing))
            return;

        if (participants.All(u => u.State <= MultiplayerUserState.Ready))
            return;

        var scores = participants.Select(u => u.Score)
                                 .Where(x => x != null).ToList();

        await All.ForEachAsync(c => c.Client.EveryoneFinished(scores));

        foreach (var user in participants.Where(u => u.Score != null))
        {
            setPlayerStatus(user.ID, MultiplayerUserState.Results);
            user.Score = null;
        }
    }

    private void setPlayerStatus(long id, MultiplayerUserState state)
    {
        Room?.SetPlayerStatus(id, state);
        _ = startIfAllReady();
    }
}
