using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using fluxel.Models.Users;
using Microsoft.EntityFrameworkCore;

namespace fluxel.Models.Groups;

[PrimaryKey(nameof(GroupID), nameof(UserID))]
[Table("group_member")]
public class GroupMember
{
    #region Columns

    [Column("group"), MaxLength(32)]
    public string GroupID { get; init; } = null!;

    [Column("user")]
    public long UserID { get; init; }

    #endregion

    #region Relations

    [ForeignKey(nameof(GroupID))]
    public Group? Group { get; init; }

    [ForeignKey(nameof(UserID))]
    public User? User { get; init; }

    #endregion
}
