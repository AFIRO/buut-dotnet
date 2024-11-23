using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rise.Domain.Bookings;
using Rise.Services.Bookings;

namespace Rise.Shared.Services;

public class DailyTaskService : IHostedService, IDisposable
{
    private Timer _timer;
    private readonly IServiceProvider _serviceProvider;

    private readonly BookingAllocationService _bookingAllocationService;

    public DailyTaskService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Calculate initial delay
        var now = DateTime.Now;
        // var nextRunTime = now.Date.AddDays(1).AddHours(20); // Next day at 20:00.
        var nextRunTime = now.Date.AddMinutes(1); // Next day at 20:00

        var initialDelay = (nextRunTime - now).TotalMilliseconds;
        
        // Ensure the delay is valid
        if (initialDelay < 0)
        {
            initialDelay = TimeSpan.FromMinutes(1).TotalMilliseconds; // Default to 24 hours
            Console.WriteLine($"Initial delay: {initialDelay} milliseconds");
        }

        _timer = new Timer(ExecuteTask, null, (long)initialDelay, (long)TimeSpan.FromMinutes(1).TotalMilliseconds);
        return Task.CompletedTask;
    }

    private async void ExecuteTask(object state)
    {
        Console.WriteLine("Start running daily task at " + DateTime.Now);

        try
        {
            await RunTaskAsync();
            
            Console.WriteLine("Running daily task at " + DateTime.Now);
            
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred while running the daily task: " + ex.Message);
        }
        // finally
        // {
        //     // Recalculate the next run time for 20:00 tomorrow
        //     var nextRunTime = DateTime.Now.Date.AddDays(1).AddHours(20);
        //     var nextDelay = (nextRunTime - DateTime.Now).TotalMilliseconds;
        //
        //     // Set the timer for the next day
        //     _timer?.Change((long)nextDelay, Timeout.Infinite);
        // }
    }
    
    private async Task RunTaskAsync()
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            // Get the scoped BookingAllocationService from the scope
            var bookingAllocationService = scope.ServiceProvider.GetRequiredService<BookingAllocationService>();

            // Allocate resources asynchronously
            await bookingAllocationService.AllocateDailyBookingAsync(DateTime.Now.Date.AddDays(5));
        }

        Console.WriteLine("Daily task completed successfully at " + DateTime.Now);

        // // Recalculate the next run time for 20:00 tomorrow
        // var nextRunTime = DateTime.Now.Date.AddDays(1).AddHours(20);
        // var nextDelay = (nextRunTime - DateTime.Now).TotalMilliseconds;
        //
        // // Set the timer for the next day
        // _timer?.Change((long)nextDelay, Timeout.Infinite);
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