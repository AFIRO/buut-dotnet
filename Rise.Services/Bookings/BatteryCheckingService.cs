using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rise.Domain.Bookings;
using Rise.Domain.Notifications;
using Rise.Persistence;
using Rise.Server.Settings;
using Rise.Services.Events;
using Rise.Services.Events.Battery;
using Rise.Shared.Enums;

namespace Rise.Services.Bookings;

/// <summary>
/// Service for checking battery holdings times and dispatching events.
/// </summary>
public class BatteryCheckingService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<BatteryCheckingService> _logger;
    private readonly IEventDispatcher _eventDispatcher;
    private readonly int _holdWarningThresholdDays;
    private readonly int _rewarningIntervalDays;

    /// <summary>
    /// Initializes a new instance of the <see cref="BatteryCheckingService"/> class. Used for checking battery holdings times and dispatching events.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="eventDispatcher">The event dispatcher.</param>
    /// <param name="logger">The logger.</param>
    public BatteryCheckingService(ApplicationDbContext dbContext, IOptions<BatterySettings> options, IEventDispatcher eventDispatcher, ILogger<BatteryCheckingService> logger)
    {
        _dbContext = dbContext;
        _eventDispatcher = eventDispatcher;
        _logger = logger;
        _holdWarningThresholdDays = options.Value.HoldWarningThresholdDays;
        _rewarningIntervalDays = options.Value.RewarningIntervalDays;
    }

    /// <summary>
    /// Checks all batteries for holding time and dispatches an event if a battery has been held for 5 days or more.
    /// </summary>
    public async Task CheckAllBatteriesForHoldingTime()
    {
        try
        {
            var batteries = await GetBatteriesWithCurrentUserAndBuutAgentAsync();
            foreach (var battery in batteries)
            {
                await CheckBatteryForHoldingTime(battery);
            }
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while checking batteries for holding time.");
        }
    }
    
    private async Task<List<Battery>> GetBatteriesWithCurrentUserAndBuutAgentAsync()
    {
        return await _dbContext.Batteries.Include(battery => battery.CurrentUser).Include(battery => battery.BatteryBuutAgent).ToListAsync();
    }

    private async Task CheckBatteryForHoldingTime(Battery battery){
        Booking? booking = await getBatteriesLastestBooking(battery);

        if(booking is null){
            return;
        }
        if(booking.BookingDate == null){
            return;
        }

        if(battery.BatteryBuutAgent is null){
            return;
        }

        if(battery.CurrentUser is null){
            return;
        }

        // booking is in the future so no problem
        if(booking.BookingDate > DateTime.Now){
            return;
        }

        // battery is held by the BUUT agent so no problem
        if(battery.CurrentUser == battery.BatteryBuutAgent){
            return;
        }

        // booking is more than 5 days ago send notification and possible last notification was more than 2 days ago
        if(IsBatteryHeldToLong(booking) && await ShouldNotificationBeSend(battery)){
            await _eventDispatcher.DispatchAsync(new BatteryTooLongWithUserEvent(battery.Id, booking.UserId, battery.BatteryBuutAgent.Id));
        }     
    }

    private bool IsBatteryHeldToLong(Booking booking)
    {
        return booking.BookingDate < DateTime.Now.AddDays(-_holdWarningThresholdDays);
    }

    private async Task<bool> ShouldNotificationBeSend(Battery battery)
    {
        Notification? latestNotification = await _dbContext.Notifications
            .Where(x => x.UserId == battery.BatteryBuutAgent.Id 
                        && x.RelatedEntityId == battery.Id 
                        && x.Type == NotificationType.Battery)
            .OrderByDescending(x => x.CreatedAt) 
            .FirstOrDefaultAsync();

        if(latestNotification is null){
            return true;
        }
        if(latestNotification.CreatedAt < DateTime.UtcNow.AddDays(-_rewarningIntervalDays)){
            return true;
        }
        return false;
    }

    private async Task<Booking?> getBatteriesLastestBooking(Battery battery)
    {
        return await _dbContext.Bookings.Where(x => x.BatteryId == battery.Id).OrderByDescending(x => x.BookingDate).FirstOrDefaultAsync();
    }
}