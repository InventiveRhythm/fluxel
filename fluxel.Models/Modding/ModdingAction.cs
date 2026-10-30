using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using fluxel.Models.Maps;
using fluxel.Models.Users;
using fluXis.Online.API.Models.Maps.Modding;
using fluXis.Online.API.Models.Users;

namespace fluxel.Models.Modding;

[Table("modding_action")]
public class ModdingAction
{
    #region Columns

    [Key, Column("id"), MaxLength(32)]
    public string ID { get; init; } = Guid.NewGuid().ToString("N");

    [Column("mapset")]
    public long MapSetID { get; init; }

    [Column("author")]
    public long AuthorID { get; init; }

    [Column("type")]
    public APIModdingActionType Type { get; init; }

    [Column("content"), MaxLength(8192)]
    public string? Content { get; init; }

    [Column("created")]
    public DateTime Created { get; init; }

    #endregion

    #region Relations

    [ForeignKey(nameof(MapSetID))]
    public MapSet? MapSet { get; init; }

    [ForeignKey(nameof(AuthorID))]
    public User? Author { get; init; }

    #endregion

    public APIModdingAction ToAPI(DatabaseContext database, Func<User?, APIUser?> convert)
    {
        database.Entry(this).Reference(x => x.Author).Load();

        return new APIModdingAction(
            ID, convert(Author) ?? APIUser.CreateUnknown(AuthorID),
            Type, APIModdingActionState.Pending, Content, new DateTimeOffset(Created).ToUnixTimeSeconds()
        );
    }
}
