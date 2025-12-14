using DSharpPlus.Entities;
using fluxel.Bot;
using fluxel.Database.Extensions;
using fluxel.Database.Helpers;
using fluxel.Modules;
using fluxel.Modules.Messages;
using fluxel.Modules.Messages.Chat;
using fluxel.Tasks.Management;
using fluxel.Utils;
using fluXis.Graphics.UserInterface.Color;
using fluXis.Online.API.Models.Users;
using Midori.Networking;
using osu.Framework.Extensions.IEnumerableExtensions;

namespace fluxel.Social;

public class NotificationsModule : IModule, IOnlineStateManager
{
    public static HttpConnectionManager<NotificationSocket> Sockets { get; private set; } = null!;

    public void OnLoad(ServerHost host)
    {
        fixInvalidOnlineStates();

        Sockets = host.Server.MapModule<NotificationSocket>("/notifications");
        host.Scheduler.Schedule(new CleanupOnlineStatesCronTask());

        createClubChannels();
    }

    private void createClubChannels()
    {
        var clubs = ClubHelper.All;

        foreach (var club in clubs)
        {
            var name = club.ChatChannel;
            var channel = ChatHelper.GetChannel(name) ?? ChatHelper.CreateClubChannel(name, club.ID);

            channel.Users.Clear();
            channel.Users.AddRange(club.Members);

            ChatHelper.Update(channel);
        }
    }

    public void OnMessage(object data)
    {
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
                var user = UserHelper.Get(onl.UserID) ?? throw new InvalidOperationException("Received online state update with non-existing user.");
                var followers = RelationHelper.GetFollowers(onl.UserID);

                Sockets.Where(s => followers.Contains(s.UserID))
                       .ForEach(s => s.Client.NotifyFriendStatus(user.ToAPI(), onl.Online));

                if (onl.Online)
                {
                    UserHelper.LogOnline(onl.UserID, true);

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
                    UserHelper.UpdateLocked(onl.UserID, u => u.LastLogin = DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                    UserHelper.LogOnline(onl.UserID, false);
                }

                break;
            }

            case ChatMessageCreateMessage cmc:
            {
                var message = ChatHelper.Get(cmc.Message);
                if (message is null) throw new InvalidOperationException();

                Sockets.ForEach(x => x.Client.ReceiveChatMessage(message.ToAPI()));

                if (message.Channel != "general" || message.DiscordID != null) return;

                var user = UserHelper.Get(message.SenderID);
                if (user is null) return; // this vexes me

                var dmsg = DiscordBot.GetChannel(DiscordBot.ChannelType.ChatLink)?.SendMessageAsync(
                    new DiscordMessageBuilder()
                        .WithEmbed(new DiscordEmbedBuilder()
                                   .WithColor(Theme.Highlight.ToDiscord())
                                   .WithAuthor(user.Username, user.Url, user.AvatarUrl)
                                   .WithDescription(message.Content)
                        )
                ).Result;

                if (dmsg is null) return;

                ChatHelper.AttachDiscordID(message.ID, dmsg.Id);
                break;
            }

            case ChatMessageDeleteMessage cmd:
            {
                var message = ChatHelper.Get(cmd.Message);
                if (message is null) throw new InvalidOperationException();

                Sockets.ForEach(c => c.Client.DeleteChatMessage(message.Channel, message.ID.ToString()));

                // delete from discord
                break;
            }
        }
    }

    public static NotificationSocket? SocketByID(long id) => Sockets.FirstOrDefault(x => x.UserID == id);

    private static void fixInvalidOnlineStates()
    {
        var online = UserHelper.LastOnlineLogs();
        online.ForEach(x => UserHelper.LogOnline(x, false));
    }

    long[] IOnlineStateManager.AllOnline => Sockets.Select(x => x.UserID).ToArray();

    bool IOnlineStateManager.IsOnline(long user) => Sockets.Any(x => x.UserID == user);

    APIActivity? IOnlineStateManager.GetActivity(long user)
    {
        var conn = Sockets.FirstOrDefault(x => x.UserID == user);
        if (conn?.Activity is null) return null;

        return new APIActivity
        {
            Name = conn.Activity.Value.name,
            Data = conn.Activity.Value.data
        };
    }
}
