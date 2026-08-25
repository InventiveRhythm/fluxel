using System;
using System.Threading.Tasks;
using Midori.Utils;
using StackExchange.Redis;

namespace fluxel.Components;

public class Valkey
{
    public static RedisChannel MapSetCreate { get; } = RedisChannel.Literal("mapset:create");
    public static RedisChannel MapSetUpdate { get; } = RedisChannel.Literal("mapset:update");

    private readonly IConnectionMultiplexer connection;

    public Valkey(IConnectionMultiplexer connection)
    {
        this.connection = connection;
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

        var parse = val.ToString().Deserialize<T>();
        if (parse is null) return;

        act(parse);
    });

    public async Task Publish<T>(RedisChannel channel, T val)
    {
        var sub = connection.GetSubscriber();
        var json = val.Serialize();
        await sub.PublishAsync(channel, json);
    }
}
