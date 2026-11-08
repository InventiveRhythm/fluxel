using System;
using System.Linq;
using System.Threading.Tasks;
using MessagePack;
using Midori.Utils;
using osu.Framework.Extensions.IEnumerableExtensions;
using StackExchange.Redis;

namespace fluxel.Components.IPC;

public class Valkey
{
    public static RedisChannel ChatMessageCreate { get; } = RedisChannel.Literal("chat:message:create");
    public static RedisChannel ChatMessageDelete { get; } = RedisChannel.Literal("chat:message:delete");

    public static RedisChannel ChatChannelMemberAdd { get; } = RedisChannel.Literal("chat:channel:member:add");
    public static RedisChannel ChatChannelMemberRemove { get; } = RedisChannel.Literal("chat:channel:member:remove");

    public static RedisChannel MapSetCreate { get; } = RedisChannel.Literal("mapset:create");
    public static RedisChannel MapSetUpdate { get; } = RedisChannel.Literal("mapset:update");

    public static RedisChannel MapRating { get; } = RedisChannel.Literal("map:rating");

    public static RedisChannel UserCreate { get; } = RedisChannel.Literal("user:create");
    public static RedisChannel UserOnline { get; } = RedisChannel.Literal("user:online");
    public static RedisChannel UserOffline { get; } = RedisChannel.Literal("user:offline");
    public static RedisChannel UserActivity { get; } = RedisChannel.Literal("user:activity");

    public static RedisKey UsersOnline { get; } = new("users:online");
    public const string USER_ACTIVITY = "users:activity";

    public IDatabase Database { get; }
    public IServer Server { get; }

    private readonly IConnectionMultiplexer connection;

    public Valkey(IConnectionMultiplexer connection)
    {
        this.connection = connection;

        Database = connection.GetDatabase();
        Server = connection.GetServer(connection.GetEndPoints().First());
    }

    public async Task<IDisposable> Subscribe(RedisChannel channel, Action<RedisChannel, RedisValue> act)
    {
        var sub = connection.GetSubscriber();
        await sub.SubscribeAsync(channel, act);
        return new InvokeOnDisposal(() => sub.Unsubscribe(channel, act));
    }

    public async Task<IDisposable> Subscribe<T>(RedisChannel channel, Action<T> act) => await Subscribe(channel, (_, val) =>
    {
        if (val == RedisValue.Null) return;

        act(MessagePackSerializer.Deserialize<T>(val));
    });

    public async Task Publish<T>(RedisChannel channel, T val)
        where T : class
    {
        var sub = connection.GetSubscriber();
        await sub.PublishAsync(channel, MessagePackSerializer.Serialize(val));
    }

    public void Cleanup(RedisValue pattern)
    {
        var keys = Server.Keys(pattern: pattern, pageSize: 99999);
        keys.ForEach(x => Database.KeyDelete(x));
    }
}
