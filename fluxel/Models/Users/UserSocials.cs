using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace fluxel.Models.Users;

[ComplexType]
public class UserSocials
{
    [Column("discord"), MaxLength(32)]
    public string? Discord { get; set; }

    [Column("twitter"), MaxLength(15)]
    public string? Twitter { get; set; }

    [Column("youtube"), MaxLength(30)]
    public string? YouTube { get; set; }

    [Column("twitch"), MaxLength(25)]
    public string? Twitch { get; set; }
}
