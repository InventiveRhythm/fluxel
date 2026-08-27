using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace fluxel.Models;

[Table("counters")]
public class Counter
{
    [Key]
    [Column("_id")]
    public CounterType Type { get; set; }

    [Column("value")]
    public long Value { get; set; }

    public long GetAndIncrease() => Value++;
}

public enum CounterType
{
    Club = 0,
    Map = 1,
    MapSet = 2,
    Score = 3,
    User = 4
}
