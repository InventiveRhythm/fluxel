using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace fluxel.Models.Auth;

[Table("oauth_app")]
public class OAuthApplication
{
    [Key, Column("id"), StringLength(32)]
    public string ID { get; init; } = Guid.NewGuid().ToString("N");

    [Column("client_id"), MaxLength(64)]
    public string ClientID { get; set; } = string.Empty;

    [Column("client_secret"), MaxLength(64)]
    public string ClientSecret { get; set; } = string.Empty;

    [Column("redirects")]
    public string[] Redirects { get; set; } = [];
}
