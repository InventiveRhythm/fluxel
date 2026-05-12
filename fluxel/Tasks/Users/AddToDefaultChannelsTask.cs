using System;
using System.Threading.Tasks;
using fluxel.Database;
using Microsoft.Extensions.DependencyInjection;

namespace fluxel.Tasks.Users;

public class AddToDefaultChannelsTask : IBasicTask
{
    public string Name => $"AddToDefaultChannels({id})";

    private long id { get; }

    public AddToDefaultChannelsTask(long id)
    {
        this.id = id;
    }

    public Task Run(IServiceProvider services)
    {
        var chats = services.GetRequiredService<ChatManager>();
        chats.AddToChannel("general", id);
        chats.AddToChannel("mapping", id);
        chats.AddToChannel("off-topic", id);
        return Task.CompletedTask;
    }
}
