using System;
using System.Collections.Generic;

namespace fluxel.Tasks;

public interface IBulkTask
{
    IEnumerable<IBasicTask> GetTasks(IServiceProvider services);
}
