using System.Diagnostics;
using fluXis.Online.API.Models.Multi;
using Microsoft.Extensions.Logging;
using Midori.Logging;

namespace fluxel.Multiplayer.Lobby;

public static class MultiplayerRoomManager
{
    public static Logger Logger { get; } = Logger.GetLogger("Multiplayer");

    private const int tick_rate = 20;
    private const int tick_sleep = 1000 / tick_rate;

    private static long idCounter = 1;
    private static bool running;

    public static List<ServerMultiplayerRoom> Lobbies { get; } = new();

    public static void StartThread()
    {
        if (running)
            throw new ArgumentException("Already running.");

        Logger.Add("Started ticking thread.");
        var thread = new Thread(threadCycle) { Name = "Multiplayer Tick" };
        thread.Start();
        running = true;
    }

    public static void StopThead()
    {
        Logger.Add("Stopping ticking thread.");
        running = false;
    }

    public static ServerMultiplayerRoom Create(MultiplayerSocket host, string name, MultiplayerPrivacy privacy, string password, long map)
    {
        var room = new ServerMultiplayerRoom(idCounter++, host, name, privacy, password, map);
        Lobbies.Add(room);
        return room;
    }

    private static void threadCycle()
    {
        while (running)
        {
            try
            {
                var sw = new Stopwatch();
                sw.Start();

                tickRooms();

                var time = (int)sw.ElapsedMilliseconds;
                var left = tick_sleep - time;

                if (left < 0)
                    Logger.Add($"Tick took {Math.Abs(left)}ms longer than expected!", LogLevel.Warning);
                else
                    Thread.Sleep(left);
            }
            catch (Exception ex)
            {
                Logger.Add("Exception occurred while ticking!", LogLevel.Error, ex);
                throw;
            }
        }
    }

    private static void tickRooms()
    {
        foreach (var room in Lobbies)
        {
            try
            {
                room.Tick();
            }
            catch (Exception ex)
            {
            }
        }
    }

    public static void Remove(ServerMultiplayerRoom room) => Lobbies.Remove(room);

    public static ServerMultiplayerRoom? FindRoom(long id)
        => Lobbies.Find(l => l.RoomID == id);

    public static ServerMultiplayerRoom? GetCurrentRoom(long id)
        => Lobbies.FirstOrDefault(l => l.Participants.Any(u => u.ID == id));
}
