using System;
using System.Collections.Generic;
using System.Linq;
using fluxel.Modules;

namespace fluxel.Components;

public class Statistics
{
    public int Online => states?.AllOnline.Length ?? 0;

    public IEnumerable<long> OnlineUsers
    {
        get
        {
            var ids = states?.AllOnline ?? Array.Empty<long>();
            return ids.Append(0);
        }
    }

    private readonly IOnlineStateManager? states;

    public Statistics(IOnlineStateManager? states = null)
    {
        this.states = states;
    }
}
