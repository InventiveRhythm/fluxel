using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using osuTK.Graphics;

namespace fluxel.Models.Groups;

[Table("group")]
public class Group
{
    #region Columns

    [Key, Column("id"), MaxLength(32)]
    public string ID { get; init; } = string.Empty;

    [Column("name"), MaxLength(32), Required]
    public string Name { get; set; } = string.Empty;

    [Column("tag"), MaxLength(5), Required]
    public string Tag { get; set; } = string.Empty;

    [Column("color")]
    public Color4 Color { get; set; } = Color4.White;

    [Column("hidden")]
    public bool Hidden { get; set; }

    #endregion

    #region Relations

    [InverseProperty(nameof(GroupMember.Group))]
    public ICollection<GroupMember> Members { get; init; } = [];

    #endregion
}
