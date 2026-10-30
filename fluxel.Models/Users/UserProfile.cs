using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using fluxel.Models.Cosmetics;

namespace fluxel.Models.Users;

[Table("user_profile")]
public class UserProfile
{
    #region Columns

    [Key, Column("id")]
    public long ID { get; init; }

    [Column("nick"), MaxLength(24)]
    public string? DisplayName { get; set; }

    [Column("avatar"), StringLength(64)]
    public string? AvatarHash { get; set; }

    [Column("avatar_animated")]
    public bool HasAnimatedAvatar { get; set; }

    [Column("banner"), StringLength(64)]
    public string? BannerHash { get; set; }

    [Column("banner_animated")]
    public bool HasAnimatedBanner { get; set; }

    [Column("aboutme"), MaxLength(512)]
    public string? AboutMe { get; set; }

    [Column("pronouns"), MaxLength(16)]
    public string? Pronouns { get; set; }

    [Column("paint"), MaxLength(32)]
    public string? NamePaintID { get; set; }

    #endregion

    #region Relations

    [ForeignKey(nameof(ID))]
    public User? User { get; init; }

    [ForeignKey(nameof(NamePaintID))]
    public NamePaint? NamePaint { get; set; }

    #endregion
}
