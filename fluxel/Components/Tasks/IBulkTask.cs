using System;
using System.Collections.Generic;

namespace fluxel.Components.Tasks;

public interface IBulkTask
{
    IEnumerable<IBasicTask> GetTasks(IServiceProvider services);
}
