using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using fluxel.Models.Clubs;
using fluxel.Models.Users;
using fluXis.Online.API.Models.Chat;
using JetBrains.Annotations;

namespace fluxel.Models.Chat;

[Table("chat_channel")]
public class ChatChannel
{
    #region Columns

    [Key, Column("id"), MaxLength(64)]
    public string ID { get; init; } = null!;

    [Column("type")]
    public APIChannelType Type { get; init; }

    [Column("dm_target_1")]
    public long? TargetUserID1 { get; init; }

    [Column("dm_target_2")]
    public long? TargetUserID2 { get; init; }

    [Column("club")]
    public long? ClubID { get; init; }

    #endregion

    #region Relations

    [ForeignKey(nameof(TargetUserID1))]
    public User? TargetUser1 { get; init; }

    [ForeignKey(nameof(TargetUserID2))]
    public User? TargetUser2 { get; init; }

    [ForeignKey(nameof(ClubID))]
    public Club? User { get; init; }

    [InverseProperty(nameof(ChatChannelMember.Channel))]
    public ICollection<ChatChannelMember> Members { get; init; } = [];

    [InverseProperty(nameof(ChatMessage.Channel))]
    public ICollection<ChatMessage> Messages { get; init; } = [];

    #endregion

    public ChatChannel(string id, APIChannelType type)
    {
        ID = id;
        Type = type;
    }

    [UsedImplicitly]
    private ChatChannel()
    {
    }
}
