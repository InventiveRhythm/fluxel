using fluxel.Database;
using fluxel.WebSocket;
using fluXis.Online.Exceptions;
using fluXis.Online.Spectator;
using fluXis.Online.Spectator.Models;
using Midori.Logging;

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

    #region Player

    public long? CurrentlyPlaying { get; private set; }
    public List<string>? CurrentMods { get; private set; }

    public Task StartSession(SpectatorState state)
    {
        CurrentlyPlaying = state.MapID ?? throw new InvalidRequestException("Missing MapID, can't start spectate session.");

        Logger.Log($"{CurrentUser.Username} started playing {CurrentlyPlaying}.");

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

        Logger.Log($"{CurrentUser.Username} sent frame bundle with {bundle.Frames.Count} frame(s).");

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

        Logger.Log($"{CurrentUser.Username} stopped playing.");
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
