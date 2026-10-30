using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using fluxel.Components;
using fluxel.Utils;
using fluXis.Online.API.Models.Maps;
using fluXis.Online.API.Models.Multi;
using fluXis.Online.API.Models.Users;
using fluXis.Scoring;
using Microsoft.Extensions.Logging;
using osu.Framework.Extensions.IEnumerableExtensions;

namespace fluxel.Services.Multiplayer.Lobby;

public class ServerMultiplayerRoom
{
    public long RoomID { get; }
    public string RoomName { get; set; }
    public string Password { get; set; }
    public MultiplayerPrivacy Privacy { get; set; }

    public long HostID { get; set; }
    public long MapID { get; set; }
    public long CountdownFinishTime { get; set; }

    public List<Participant> Participants { get; } = new();
    public List<(long, int)> ScheduledScores { get; } = new();
    public List<string> CurrentMods { get; set; } = new();

    public IEnumerable<MultiplayerSocket> All => Participants.Select(x => x.Socket);

    public readonly SemaphoreSlim RoomLock = new(1, 1);

    public ServerMultiplayerRoom(long id, MultiplayerSocket host, string name, MultiplayerPrivacy privacy, string password, long map)
    {
        RoomID = id;
        HostID = host.UserID;
        RoomName = name;
        Privacy = privacy;
        Password = password;
        MapID = map;

        Participants.Add(new Participant(host));
    }

    public async void Tick()
    {
        await RoomLock.WaitAsync();

        try
        {
            await processCountdown();

            var top = ScheduledScores.OrderByDescending(x => x.Item2).DistinctBy(x => x.Item1).ToList();
            ScheduledScores.Clear();

            foreach (var (user, score) in top)
                await All.ForEachAsync(c => c.Client.ScoreUpdated(user, score));
        }
        catch (Exception ex)
        {
            MultiplayerRoomManager.Logger.Add($"Failed to tick room {RoomID} '{RoomName}'!", LogLevel.Error, ex);
        }
        finally
        {
            RoomLock.Release();
        }
    }

    private async Task processCountdown()
    {
        if (CountdownFinishTime == 0)
            return;

        var time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        if (Participants.Any(x => x.State != MultiplayerUserState.Ready))
        {
            await All.ForEachAsync(c => c.Client.CountdownStarted(null));
            return;
        }

        if (time < CountdownFinishTime)
            return;

        foreach (var user in Participants)
            SetPlayerStatus(user.ID, MultiplayerUserState.Playing);

        await All.ForEachAsync(c => c.Client.LoadRequested());
    }

    public void SetPlayerStatus(long id, MultiplayerUserState state)
    {
        var user = GetPlayer(id);

        if (user == null)
            return;

        user.State = state;
        All.ForEach(c => c.Client.UserStateChanged(id, state));
    }

    public void RunLocked(Action action)
    {
        RoomLock.Wait();

        try
        {
            action?.Invoke();
        }
        finally
        {
            RoomLock.Release();
        }
    }

    public bool HasPlayer(long id) => Participants.Any(u => u.ID == id);
    public Participant? GetPlayer(long id) => Participants.FirstOrDefault(u => u.ID == id);

    public MultiplayerRoom ToAPI(ModelTranslator translator) => new()
    {
        RoomID = RoomID,
        Name = RoomName,
        Privacy = Privacy,
        Host = translator.ToAPI(translator.Database.Users.Find(HostID)) ?? APIUser.CreateUnknown(HostID),
        Participants = [.. Participants.Select(x => x.ToAPI(translator))],
        Map = translator.ToAPI(translator.Database.Maps.Find(MapID)) ?? APIMap.CreateUnknown(MapID)
    };

    public class Participant
    {
        public MultiplayerSocket Socket { get; }
        public long ID => Socket.UserID;

        public MultiplayerUserState State { get; set; } = MultiplayerUserState.Idle;
        public ScoreInfo? Score { get; set; }

        public Participant(MultiplayerSocket sock)
        {
            Socket = sock;
        }

        public MultiplayerParticipant ToAPI(ModelTranslator translator) => new()
        {
            Player = translator.ToAPI(translator.Database.Users.Find(ID)) ?? APIUser.CreateUnknown(ID),
            State = State
        };
    }
}
