using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using osu.Framework.Extensions.IEnumerableExtensions;

namespace fluxel.Tasks;

public class TaskRunner : BackgroundService
{
    private readonly ILogger logger;
    private readonly IServiceProvider services;

    private List<IBasicTask> tasks { get; } = new();
    private List<ICronTask> cron { get; } = new();

    private object @lock { get; } = new { };

    public IReadOnlyList<IBasicTask> Queue => tasks;

    public TaskRunner(ILoggerFactory loggerFactory, IServiceProvider services)
    {
        logger = loggerFactory.CreateLogger("Tasks");
        this.services = services;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting Task Runner.");
        return loop(stoppingToken);
    }

    public void Schedule(IBulkTask task)
        => task.GetTasks(services).ForEach(Schedule);

    public void Schedule(IBasicTask task)
    {
        lock (@lock)
        {
            if (task is ICronTask ct)
            {
                ct.Valid = true;
                cron.Add(ct);
            }
            else
                tasks.Add(task);
        }
    }

    private async Task loop(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            IBasicTask? task = null;

            try
            {
                lock (@lock)
                {
                    var time = DateTimeOffset.Now;

                    foreach (var ct in cron)
                    {
                        if (time.Hour == ct.Hour && time.Minute == ct.Minute)
                        {
                            if (!ct.Valid)
                                continue;

                            task = ct;
                            ct.Valid = false;
                        }
                        else
                            ct.Valid = true;
                    }

                    if (task is null && tasks.Count > 0)
                    {
                        task = tasks[0];
                        tasks.RemoveAt(0);
                    }
                }

                task?.Run(services);
            }
            catch (Exception ex)
            {
                var name = task?.Name ?? task?.GetType().Name ?? "unknown";
                logger.LogError(ex, $"An error occurred while running task '{name}'.");
            }
            finally
            {
                // If there are no tasks, wait 1 second before checking again.
                if (tasks.Count == 0)
                    await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
