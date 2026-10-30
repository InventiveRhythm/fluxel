using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using fluxel.Models.Users;
using Microsoft.EntityFrameworkCore;

namespace fluxel.Models.Collections;

[Index(nameof(OwnerID))]
[Table("collection")]
public class Collection
{
    #region Columns

    [Key, Column("id"), StringLength(32)]
    public string ID { get; init; } = Guid.NewGuid().ToString("N");

    [Column("name"), MaxLength(64), Required]
    public string Name { get; set; } = string.Empty;

    [Column("description"), MaxLength(4096)]
    public string Description { get; set; } = string.Empty;

    [Column("owner")]
    public long OwnerID { get; init; }

    [Column("created")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    [Column("updated")]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    #endregion

    #region Relations

    [ForeignKey(nameof(OwnerID))]
    public User? Owner { get; init; }

    [InverseProperty(nameof(CollectionEntry.Collection))]
    public ICollection<CollectionEntry> Entries { get; init; } = [];

    #endregion
}
