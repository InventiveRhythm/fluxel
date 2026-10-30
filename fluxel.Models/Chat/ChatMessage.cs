using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using fluxel.Models.Users;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace fluxel.Models.Chat;

[Index(nameof(ChannelID))]
[Index(nameof(DiscordID), IsUnique = true)]
[Table("chat_message")]
public class ChatMessage
{
    #region Columns

    [Key, Column("id"), StringLength(32)]
    public string ID { get; init; } = Guid.NewGuid().ToString("N");

    [Column("discord")]
    public ulong? DiscordID { get; set; }

    [Column("created")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    [Column("deleted")]
    public DateTime? DeletedAt { get; set; }

    [Column("author"), Required]
    public long AuthorID { get; init; }

    [Column("content"), MaxLength(4096)]
    public string Content { get; init; } = string.Empty;

    [Column("channel"), MaxLength(64), Required]
    public string ChannelID { get; init; } = string.Empty;

    #endregion

    #region Relations

    [ForeignKey(nameof(AuthorID))]
    public User? Author { get; init; }

    [ForeignKey(nameof(ChannelID))]
    public ChatChannel? Channel { get; init; }

    #endregion

    public ChatMessage(string channel, long author, string content)
    {
        ChannelID = channel;
        AuthorID = author;
        Content = content;
    }

    [UsedImplicitly]
    private ChatMessage() { }
}
