using System;
using Midori.Utils;
using Newtonsoft.Json;

namespace fluxel.IPC;

public class MapSetIDEvent
{
    public long ID { get; set; }

    public MapSetIDEvent(long id)
    {
        ID = id;
    }

    [JsonConstructor]
    [Obsolete(JsonUtils.JSON_CONSTRUCTOR_ERROR)]
    public MapSetIDEvent()
    {
    }
}
