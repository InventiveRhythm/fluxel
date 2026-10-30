using System;
using System.Threading.Tasks;

namespace fluxel.Components.Tasks;

public interface IBasicTask
{
    string Name { get; }
    Task Run(IServiceProvider services);
}
