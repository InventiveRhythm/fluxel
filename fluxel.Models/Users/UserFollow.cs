using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace fluxel.Models.Users;

[PrimaryKey(nameof(FollowerID), nameof(FolloweeID))]
[Table("user_follow")]
public class UserFollow
{
    [Column("follower")]
    public long FollowerID { get; init; }

    [Column("followee")]
    public long FolloweeID { get; init; }

    [ForeignKey(nameof(FollowerID))]
    public User? Follower { get; init; }

    [ForeignKey(nameof(FolloweeID))]
    public User? Followee { get; init; }
}
