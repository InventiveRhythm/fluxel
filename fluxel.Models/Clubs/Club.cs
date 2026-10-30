using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using fluxel.Models.Users;
using fluXis.Online.API.Models.Clubs;
using Microsoft.EntityFrameworkCore;
using osuTK.Graphics;

namespace fluxel.Models.Clubs;

[Index(nameof(Tag), Name = "tag", IsUnique = true)]
[Table("club")]
public class Club
{
    #region Columns

    [Key, Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long ID { get; init; }

    [Column("name"), MaxLength(24), Required]
    public string Name { get; set; } = string.Empty;

    [Column("tag"), MinLength(3), MaxLength(5), Required]
    public string Tag { get; set; } = string.Empty;

    [Column("icon"), StringLength(64)]
    public string? IconHash { get; set; }

    [Column("banner"), StringLength(64)]
    public string? BannerHash { get; set; }

    [Column("join_type")]
    public ClubJoinType JoinType { get; set; }

    [Column("color_start")]
    public Color4 ColorStart { get; set; } = Color4.White;

    [Column("color_end")]
    public Color4 ColorEnd { get; set; } = Color4.White;

    [Column("owner"), Required]
    public long OwnerID { get; set; }

    #endregion

    #region Relations

    [ForeignKey(nameof(OwnerID))]
    public User? Owner { get; init; }

    [InverseProperty(nameof(ClubStatistics.Club))]
    public ICollection<ClubStatistics> Statistics { get; init; } = [];

    [InverseProperty(nameof(User.Club))]
    public ICollection<User> Members { get; init; } = [];

    #endregion

    [NotMapped]
    public string ChatChannelID => $"club_{ID}";
}
