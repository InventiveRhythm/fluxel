using System;
using System.Threading.Tasks;

namespace fluxel.Tasks.Other;

public class MethodTask : IBasicTask
{
    public string Name => $"MethodTask({function.Method})";

    private Delegate function { get; }

    public MethodTask(Delegate function)
    {
        this.function = function;
    }

    public Task Run(IServiceProvider services)
    {
        function.DynamicInvoke();
        return Task.CompletedTask;
    }
}
