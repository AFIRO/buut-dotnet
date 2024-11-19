using Microsoft.Extensions.Hosting;

namespace Rise.Shared.Services;

public class DailyTaskService : IHostedService, IDisposable
{
    private Timer _timer;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Calculate initial delay
        var now = DateTime.Now;
        var nextRunTime = now.Date.AddDays(1).AddHours(20); // Next day at 20:00
        var initialDelay = (nextRunTime - now).TotalMilliseconds;
        
        // Ensure the delay is valid
        if (initialDelay < 0)
        {
            initialDelay = TimeSpan.FromDays(1).TotalMilliseconds; // Default to 24 hours
            Console.WriteLine($"Initial delay: {initialDelay} milliseconds");
        }

        _timer = new Timer(ExecuteTask, null, (long)initialDelay, (long)TimeSpan.FromMinutes(1).TotalMilliseconds);
        return Task.CompletedTask;
    }

    private void ExecuteTask(object state)
    {
        // Your daily task logic here
        Console.WriteLine("Running daily task at " + DateTime.Now);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}