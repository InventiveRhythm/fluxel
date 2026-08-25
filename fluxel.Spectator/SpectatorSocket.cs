using fluxel.Database;
using fluxel.WebSocket;
using fluXis.Online.Exceptions;
using fluXis.Online.Spectator;
using fluXis.Online.Spectator.Models;

namespace fluxel.Spectator;

public class SpectatorSocket : AuthenticatedSocket<ISpectatorServer, ISpectatorClient>, ISpectatorServer
{
    private SpectatorSocket[] viewers => module.Sockets.Where(x => x.CurrentlyWatching.Contains(UserID)).ToArray();

    private readonly SpectatorModule module;

    public SpectatorSocket(SpectatorModule module, UserManager users)
        : base(users)
    {
        this.module = module;
    }

    protected override void OnClose()
    {
        base.OnClose();

        EndSession();
        CurrentlyWatching.Clear();
    }

    #region Player

    public long? CurrentlyPlaying { get; private set; }

    public Task StartSession(SpectatorState state)
    {
        CurrentlyPlaying = state.MapID ?? throw new InvalidRequestException("Missing MapID, can't start spectate session.");

        foreach (var viewer in viewers)
        {
            _ = viewer.Client.StartedPlaying(UserID, state);
        }

        return Task.CompletedTask;
    }

    public Task SendFrameBundle(SpectatorFrameBundle bundle)
    {
        if (CurrentlyPlaying is null)
            return Task.CompletedTask;

        foreach (var viewer in viewers)
        {
            _ = viewer.Client.ReceiveFrameBundle(UserID, bundle);
        }

        return Task.CompletedTask;
    }

    public Task EndSession()
    {
        if (CurrentlyPlaying is null)
            return Task.CompletedTask;

        CurrentlyPlaying = null;

        foreach (var viewer in viewers)
        {
            _ = viewer.Client.StoppedPlaying(UserID);
        }

        return Task.CompletedTask;
    }

    #endregion

    #region Viewer

    public List<long> CurrentlyWatching { get; } = new();

    public Task StartWatching(long id)
    {
        CurrentlyWatching.Add(id);
        return Task.CompletedTask;
    }

    public Task StopWatching(long id)
    {
        CurrentlyWatching.RemoveAll(x => x == id);
        return Task.CompletedTask;
    }

    #endregion
}
