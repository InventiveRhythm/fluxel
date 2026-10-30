using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using fluxel.Models.Users;
using JetBrains.Annotations;

namespace fluxel.Models.Auth;

[Table("auth")]
public class AuthInfo
{
    #region Columns

    [Column("user"), Required]
    public long UserID { get; init; }

    #endregion

    #region Relations

    [ForeignKey(nameof(UserID))]
    public User? User { get; init; }

    #endregion

    public AuthInfo(User user)
    {
        UserID = user.ID;
        User = user;
    }

    [UsedImplicitly]
    protected AuthInfo()
    {
    }
}
