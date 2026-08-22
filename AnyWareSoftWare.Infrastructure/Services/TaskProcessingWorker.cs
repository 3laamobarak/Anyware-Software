using System;
using System.Threading;
using System.Threading.Tasks;
using AnyWareSoftWare.Application.Interfaces;
using AnyWareSoftWare.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AnyWareSoftWare.Infrastructure.Services
{
    public class TaskProcessingWorker : BackgroundService
    {
        private readonly IBackgroundQueue _queue;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TaskProcessingWorker> _logger;

        public TaskProcessingWorker(IBackgroundQueue queue, IServiceProvider serviceProvider, ILogger<TaskProcessingWorker> logger)
        {
            _queue = queue;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Background Task Processing Worker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var taskId = await _queue.DequeueAsync(stoppingToken);

                    await Task.Delay(1000, stoppingToken);

                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var task = await dbContext.Tasks.FindAsync(new object[] { taskId }, stoppingToken);
                    if (task != null)
                    {
                        task.Status = Domain.Enums.TaskStatus.InProgress;
                        await dbContext.SaveChangesAsync(stoppingToken);
                        _logger.LogInformation($"Processed and updated task ID: {taskId}");
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing task.");
                }
            }
        }
    }
}