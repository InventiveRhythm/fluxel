using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using fluxel.Models.Users;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace fluxel.Models.Chat;

[PrimaryKey(nameof(ChannelID), nameof(UserID))]
[Table("chat_channel_member")]
public class ChatChannelMember
{
    #region Columns

    [Column("channel"), MaxLength(64)]
    public string ChannelID { get; init; } = null!;

    [Column("user")]
    public long UserID { get; init; }

    #endregion

    #region Relations

    [ForeignKey(nameof(ChannelID))]
    public ChatChannel? Channel { get; init; }

    [ForeignKey(nameof(UserID))]
    public User? User { get; init; }

    #endregion

    public ChatChannelMember(string channelID, long userID)
    {
        ChannelID = channelID;
        UserID = userID;
    }

    public ChatChannelMember(ChatChannel channel, User user)
    {
        Channel = channel;
        User = user;

        ChannelID = channel.ID;
        UserID = user.ID;
    }

    [UsedImplicitly]
    private ChatChannelMember()
    {
    }
}
