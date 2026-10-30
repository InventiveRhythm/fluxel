using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using fluxel.Models.Users;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace fluxel.Models.Auth;

[Index(nameof(Email))]
public class LegacyAuthInfo : AuthInfo
{
    [Column("email"), MaxLength(256), Required]
    public string? Email { get; set; }

    [Column("password"), MaxLength(256), Required]
    public string Password { get; set; } = string.Empty;

    public LegacyAuthInfo(User user, string? email, string password)
        : base(user)
    {
        Email = email;
        Password = password;
    }

    [UsedImplicitly]
    private LegacyAuthInfo()
    {
    }
}
