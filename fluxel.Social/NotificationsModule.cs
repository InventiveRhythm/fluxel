using DSharpPlus.Entities;
using fluxel.Bot;
using fluxel.Components;
using fluxel.Database;
using fluxel.Modules;
using fluxel.Modules.Messages;
using fluxel.Modules.Messages.Chat;
using fluxel.Tasks;
using fluxel.Tasks.Management;
using fluxel.Utils;
using fluXis.Graphics.UserInterface.Color;
using fluXis.Online.API.Models.Users;
using Microsoft.Extensions.DependencyInjection;
using Midori.Networking;
using osu.Framework.Extensions.IEnumerableExtensions;

namespace fluxel.Social;

public class NotificationsModule : IModule, IOnlineStateManager
{
    public HttpConnectionManager<NotificationSocket> Sockets { get; }

    private readonly UserManager users;
    private readonly ClubManager clubs;
    private readonly ChatManager chat;
    private readonly DiscordBot discord;
    private readonly UrlFormatter urls;
    private readonly IServiceProvider services;

    public NotificationsModule(UserManager users, DiscordBot discord, ClubManager clubs, ChatManager chat, UrlFormatter urls, HttpRouter router, TaskRunner tasks, IServiceProvider services)
    {
        this.users = users;
        this.discord = discord;
        this.clubs = clubs;
        this.chat = chat;
        this.urls = urls;
        this.services = services;

        Sockets = router.MapModule<NotificationSocket>("/notifications", manager: true)!;

        tasks.Schedule(new CleanupOnlineStatesCronTask(), DateTime.Today, TimeSpan.FromDays(1));
        fixInvalidOnlineStates();
        createClubChannels();
    }

    private void createClubChannels()
    {
        var c = clubs.All;

        foreach (var club in c)
        {
            var name = club.ChatChannel;
            var channel = chat.GetChannel(name) ?? chat.CreateClubChannel(name, club.ID);

            channel.Users.Clear();
            channel.Users.AddRange(club.Members);

            chat.Update(channel);
        }
    }

    public void OnMessage(object data)
    {
        var translator = services.CreateScope().ServiceProvider.GetRequiredService<ModelTranslator>();

        switch (data)
        {
            case UserCollectionMessage coll:
            {
                Sockets.Where(x => x.UserID == coll.UserID).ForEach(x => x.Client.CollectionUpdated(coll.CollectionID, coll.Added, coll.Changed, coll.Removed));
                break;
            }

            case UserAchievementMessage ach:
            {
                Sockets.Where(x => x.UserID == ach.UserID).ForEach(x => x.Client.RewardAchievement(ach.Achievement));
                break;
            }

            case UserNotificationMessage notif:
            {
                Sockets.Where(x => x.UserID == notif.UserID).ForEach(x => x.Client.NotificationReceived(notif.Notification));
                break;
            }

            case UserOnlineStateMessage onl:
            {
                var user = users.Get(onl.UserID) ?? throw new InvalidOperationException("Received online state update with non-existing user.");
                var followers = users.GetFollowers(onl.UserID);

                Sockets.Where(s => followers.Contains(s.UserID))
                       .ForEach(s => s.Client.NotifyFriendStatus(translator.ToAPI(user), onl.Online));

                if (onl.Online)
                {
                    users.LogOnline(onl.UserID, true);

                    if (Sockets.Count(x => x.UserID == onl.UserID) > 1)
                    {
                        var connections = Sockets.Where(x => x.UserID == onl.UserID).ToList();
                        var lastConnection = connections.OrderBy(x => x.StartTime).First();
                        lastConnection.Client.Logout("Logged in from another location.");
                        // potentially force disconnect
                    }
                }
                else
                {
                    users.UpdateLocked(onl.UserID, u => u.LastLogin = DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                    users.LogOnline(onl.UserID, false);
                }

                break;
            }

            case ChatMessageCreateMessage cmc:
            {
                var message = chat.Get(cmc.Message);
                if (message is null) throw new InvalidOperationException();

                Sockets.ForEach(x => x.Client.ReceiveChatMessage(translator.ToAPI(message)));

                if (message.Channel != "general" || message.DiscordID != null) return;

                var user = users.Get(message.SenderID);
                if (user is null) return; // this vexes me

                var dmsg = discord.GetChannel(DiscordBot.ChannelType.ChatLink)?.SendMessageAsync(
                    new DiscordMessageBuilder()
                        .WithEmbed(new DiscordEmbedBuilder { Author = user.ToEmbedAuthor(urls) }
                                   .WithColor(Theme.Highlight.ToDiscord())
                                   .WithDescription(message.Content)
                        )
                ).Result;

                if (dmsg is null) return;

                chat.AttachDiscordID(message.ID, dmsg.Id);
                break;
            }

            case ChatMessageDeleteMessage cmd:
            {
                var message = chat.Get(cmd.Message);
                if (message is null) throw new InvalidOperationException();

                Sockets.ForEach(c => c.Client.DeleteChatMessage(message.Channel, message.ID.ToString()));

                // delete from discord
                break;
            }
        }
    }

    public NotificationSocket? SocketByID(long id) => Sockets.FirstOrDefault(x => x.UserID == id);

    private void fixInvalidOnlineStates()
    {
        var online = users.LastOnlineLogs();
        online.ForEach(x => users.LogOnline(x, false));
    }

    long[] IOnlineStateManager.AllOnline => Sockets.Select(x => x.UserID).ToArray();

    bool IOnlineStateManager.IsOnline(long user) => Sockets.Any(x => x.UserID == user);

    APIActivity? IOnlineStateManager.GetActivity(long user)
        => Sockets.FirstOrDefault(x => x.UserID == user)?.Activity;
}
