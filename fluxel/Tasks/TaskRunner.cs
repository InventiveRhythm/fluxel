using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using osu.Framework.Extensions.IEnumerableExtensions;

namespace fluxel.Tasks;

public class TaskRunner : BackgroundService
{
    private readonly ILogger logger;
    private readonly IServiceProvider services;

    private ConcurrentBag<ScheduledTask> tasks { get; } = new();
    public IReadOnlyList<ScheduledTask> Queue => tasks.ToList();

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
        => task.GetTasks(services).ForEach(t => Schedule(t));

    public void Schedule(IBasicTask task, DateTime? next = null, TimeSpan? interval = null)
        => tasks.Add(new ScheduledTask(task, interval, next));

    private async Task loop(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            ScheduledTask? task = null;

            try
            {
                var time = DateTime.Now;
                task = tasks.FirstOrDefault(x => x.NextRun <= time);
                if (task == null) continue;

                logger.LogInformation("Running '{t}', scheduled at {s}.", task.Task.Name, task.NextRun);

                if (task.Interval != null)
                {
                    task.NextRun = task.NextRun.Add(task.Interval.Value);
                    logger.LogInformation("Next run is at {s}.", task.NextRun);
                }

                await task.Task.Run(services.CreateScope().ServiceProvider);
            }
            catch (Exception ex)
            {
                var name = task?.Task.Name ?? task?.GetType().Name ?? "unknown";
                logger.LogError(ex, $"An error occurred while running task '{name}'.");
            }
            finally
            {
                // If there are no tasks, wait 1 second before checking again.
                if (tasks.IsEmpty)
                    await Task.Delay(1000, stoppingToken);
            }
        }
    }

    public class ScheduledTask
    {
        public IBasicTask Task { get; }
        public TimeSpan? Interval { get; }
        public DateTime NextRun { get; set; }

        public ScheduledTask(IBasicTask task, TimeSpan? interval = null, DateTime? next = null)
        {
            Task = task;
            Interval = interval;
            NextRun = next ?? DateTime.Now;
        }
    }
}
