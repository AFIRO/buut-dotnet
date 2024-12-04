using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rise.Domain.Bookings;
using Rise.Services.Bookings;

namespace Rise.Shared.Services;

/// <summary>
/// Service for executing daily tasks.
/// </summary>
public class DailyTaskService : IHostedService, IDisposable
{
    private Timer? _timer;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DailyTaskService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DailyTaskService"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="logger">The logger instance.</param>
    public DailyTaskService(IServiceProvider serviceProvider, ILogger<DailyTaskService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }


    /// <summary>
    /// Starts the daily task service.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Calculate initial delay
        var now = DateTime.Now;
        // var nextRunTime = now.Date.AddDays(1).AddHours(20); // Next day at 20:00.
        var nextRunTime = now.Date.AddMinutes(1);

        var initialDelay = (nextRunTime - now).TotalMilliseconds;

        // Ensure the delay is valid
        if (initialDelay < 0)
        {
            // initialDelay = TimeSpan.FromDays(1).TotalMilliseconds; // Default to 24 hours
            initialDelay = TimeSpan.FromMinutes(1).TotalMilliseconds; // Default to 1 minute

            _logger.LogWarning("Initial delay: {InitialDelay} milliseconds", initialDelay);
        }

        // _timer = new Timer(ExecuteTask, null, (long)initialDelay, (long)TimeSpan.FromDays(1).TotalMilliseconds);
        _timer = new Timer(ExecuteTask, null, (long)initialDelay, (long)TimeSpan.FromMinutes(1).TotalMilliseconds);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Executes the daily task.
    /// </summary>
    /// <param name="state">The state object passed to the timer.</param>
    private async void ExecuteTask(object? state)
    {
        _logger.LogInformation("Start running daily task at {DateTime}", DateTime.Now);

        try
        {
            _logger.LogInformation("Running daily task at {DateTime}", DateTime.Now);
            await RunTaskAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while running the daily task");
        }
    }

    /// <summary>
    /// Runs the daily task asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task RunTaskAsync()
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            // Get the scoped BookingAllocationService from the scope
            var bookingAllocationService = scope.ServiceProvider.GetRequiredService<BookingAllocationService>();

            // Allocate resources asynchronously
            await bookingAllocationService.AllocateDailyBookingAsync(DateTime.Now.Date.AddDays(5));
        }
        _logger.LogInformation("Daily task completed successfully at {DateTime}", DateTime.Now);
    }

    /// <summary>
    /// Stops the daily task service.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Disposes the timer.
    /// </summary>
    public void Dispose()
    {
        _timer?.Dispose();
    }
}