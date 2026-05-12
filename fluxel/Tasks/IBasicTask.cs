using System;
using System.Threading.Tasks;

namespace fluxel.Tasks;

public interface IBasicTask
{
    string Name { get; }
    Task Run(IServiceProvider services);
}
