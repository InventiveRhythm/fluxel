using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JetBrains.Annotations;

namespace fluxel.Models.Cosmetics;

[Table("name_paint")]
public class NamePaint
{
    [Key, Column("id"), MaxLength(32)]
    public string ID { get; init; } = string.Empty;

    [Column("name"), MaxLength(32), Required]
    public string Name { get; init; } = string.Empty;

    public NamePaint(string id, string name)
    {
        ID = id;
        Name = name;
    }

    [UsedImplicitly]
    private NamePaint()
    {
    }
}
