using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using fluxel.Models.Maps;
using Microsoft.EntityFrameworkCore;

namespace fluxel.Models.Collections;

[PrimaryKey(nameof(CollectionID), nameof(MapID))]
[Table("collection_entry")]
public class CollectionEntry
{
    #region Columns

    [Column("collection"), StringLength(32)]
    public string CollectionID { get; init; } = string.Empty;

    [Column("map")]
    public long MapID { get; set; }

    #endregion

    #region Relations

    [ForeignKey(nameof(CollectionID))]
    public Collection? Collection { get; init; }

    [ForeignKey(nameof(MapID))]
    public Map? Map { get; init; }

    #endregion
}
