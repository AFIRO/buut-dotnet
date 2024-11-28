using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;
using Rise.Domain.Bookings;
using Rise.Persistence;
using Rise.Server.Settings;
using Rise.Services.Bookings;

namespace Rise.Services.Tests.Bookings;

public class BookingAllocationServiceTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly BookingAllocationService _service;

    public BookingAllocationServiceTests()
    {
        // Configure an in-memory database
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString());
        _dbContext = new ApplicationDbContext(optionsBuilder.Options);

        // Configure BookingSettings
        var bookingSettings = new BookingSettings
        {
            MinReservationDays = 1,
            MaxReservationDays = 30
        };
        var options = Options.Create(bookingSettings);

        // Initialize the service
        _service = new BookingAllocationService(new BookingAllocator(), _dbContext, options);
    }

    [Fact]
    public async Task AllocateDailyBookingAsync_ShouldAllocateBookings()
    {
        // Arrange
        var date = DateTime.Today;

        var bookings = new List<Booking>
        {
            new Booking(date, "User1"),
            new Booking(date, "User2")
        };
        var batteries = new List<Battery>
        {
            new Battery("Battery1"),
            new Battery("Battery2")
        };
        var boats = new List<Boat>
        {
            new Boat("Boat1"),
            new Boat("Boat2")
        };

        // Seed data into the in-memory database
        await _dbContext.Bookings.AddRangeAsync(bookings);
        await _dbContext.Batteries.AddRangeAsync(batteries);
        await _dbContext.Boats.AddRangeAsync(boats);
        await _dbContext.SaveChangesAsync();

        // Act
        await _service.AllocateDailyBookingAsync(date);

        // Assert
        var allocatedBookings = _dbContext.Bookings.ToList();
        Assert.All(allocatedBookings, b =>
        {
            Assert.NotNull(b.Battery);
            Assert.NotNull(b.Boat);
        });
    }

    [Fact]
    public async Task AllocateDailyBookingAsync_ShouldNotAllocateForEmptyBookings()
    {
        // Arrange
        var date = DateTime.Today;

        // No bookings, only resources
        var batteries = new List<Battery>
        {
            new Battery("Battery1"),
            new Battery("Battery2")
        };
        var boats = new List<Boat>
        {
            new Boat("Boat1"),
            new Boat("Boat2")
        };

        await _dbContext.Batteries.AddRangeAsync(batteries);
        await _dbContext.Boats.AddRangeAsync(boats);
        await _dbContext.SaveChangesAsync();

        // Act
        await _service.AllocateDailyBookingAsync(date);

        // Assert
        var allocatedBookings = _dbContext.Bookings.ToList();
        Assert.Empty(allocatedBookings);
    }
    
    [Fact]
    public async Task AllocateDailyBookingAsync_ShouldSkipPastBookings()
    {
        // Arrange
        var today = DateTime.Today;
        var pastDate = today.AddDays(-1);

        var bookings = new List<Booking>
        {
            new Booking(pastDate, "User1"), // Past booking
            new Booking(today, "User2")    // Today's booking
        };
        var batteries = new List<Battery>
        {
            new Battery("Battery1"),
            new Battery("Battery2")
        };
        var boats = new List<Boat>
        {
            new Boat("Boat1"),
            new Boat("Boat2")
        };

        // Seed data
        await _dbContext.Bookings.AddRangeAsync(bookings);
        await _dbContext.Batteries.AddRangeAsync(batteries);
        await _dbContext.Boats.AddRangeAsync(boats);
        await _dbContext.SaveChangesAsync();

        // Act
        await _service.AllocateDailyBookingAsync(today);

        // Assert
        var allocatedBookings = _dbContext.Bookings.ToList();
        Assert.Null(allocatedBookings[0].Battery); // Past booking should not be allocated
        Assert.Null(allocatedBookings[0].Boat);
        Assert.NotNull(allocatedBookings[1].Battery); // Today's booking should be allocated
        Assert.NotNull(allocatedBookings[1].Boat);
    }
    
    [Fact]
    public async Task AllocateDailyBookingAsync_ShouldHandleOverlappingBookings()
    {
        // Arrange
        var today = DateTime.Today;

        var bookings = new List<Booking>
        {
            new Booking(today.AddHours(9), "User1"),  // Morning booking
            new Booking(today.AddHours(15), "User2") // Afternoon booking
        };
        var batteries = new List<Battery>
        {
            new Battery("Battery1"),
            new Battery("Battery2")
        };
        var boats = new List<Boat>
        {
            new Boat("Boat1"),
            new Boat("Boat2")
        };

        // Seed data
        await _dbContext.Bookings.AddRangeAsync(bookings);
        await _dbContext.Batteries.AddRangeAsync(batteries);
        await _dbContext.Boats.AddRangeAsync(boats);
        await _dbContext.SaveChangesAsync();

        // Act
        await _service.AllocateDailyBookingAsync(today);

        // Assert
        var allocatedBookings = _dbContext.Bookings.OrderBy(b => b.BookingDate).ToList();
        Assert.Equal("Battery1", allocatedBookings[0].Battery.Name); // First booking gets first battery
        Assert.Equal("Boat1", allocatedBookings[0].Boat.Name);

        Assert.Equal("Battery2", allocatedBookings[1].Battery.Name); // Second booking gets second battery
        Assert.Equal("Boat2", allocatedBookings[1].Boat.Name);
    }

    [Fact]
    public async Task AllocateDailyBookingAsync_ShouldHandleInsufficientResources()
    {
        // Arrange
        var today = DateTime.Today;

        var bookings = new List<Booking>
        {
            new Booking(today, "User1"),
            new Booking(today, "User2"),
            new Booking(today, "User3") // No resources available for this booking
        };
        var batteries = new List<Battery>
        {
            new Battery("Battery1"),
            new Battery("Battery2")
        };
        var boats = new List<Boat>
        {
            new Boat("Boat1"),
            new Boat("Boat2")
        };

        // Seed data
        await _dbContext.Bookings.AddRangeAsync(bookings);
        await _dbContext.Batteries.AddRangeAsync(batteries);
        await _dbContext.Boats.AddRangeAsync(boats);
        await _dbContext.SaveChangesAsync();

        // Act
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _service.AllocateDailyBookingAsync(today));

        // Assert
        var allocatedBookings = _dbContext.Bookings.ToList();
        Assert.NotNull(allocatedBookings[0].Battery); // First booking is allocated
        Assert.NotNull(allocatedBookings[0].Boat);

        Assert.NotNull(allocatedBookings[1].Battery); // Second booking is allocated
        Assert.NotNull(allocatedBookings[1].Boat);

        Assert.Null(allocatedBookings[2].Battery); // Third booking cannot be allocated
        Assert.Null(allocatedBookings[2].Boat);
    }

    [Fact]
    public async Task AllocateDailyBookingAsync_ShouldNotReallocateAlreadyAllocatedBookings()
    {
        // Arrange
        var today = DateTime.Today;

        var batteries = new List<Battery>
        {
            new Battery("Battery1"),
            new Battery("Battery2")
        };
        var boats = new List<Boat>
        {
            new Boat("Boat1"),
            new Boat("Boat2")
        };
        var bookings = new List<Booking>
        {
            new Booking(today.AddHours(14), "User1") { Battery = batteries[0], Boat = boats[0] }, // Already allocated
            new Booking(today.AddHours(10), "User2")
        };

        // Seed data
        await _dbContext.Bookings.AddRangeAsync(bookings);
        await _dbContext.Batteries.AddRangeAsync(batteries);
        await _dbContext.Boats.AddRangeAsync(boats);
        await _dbContext.SaveChangesAsync();

        // Act
        await _service.AllocateDailyBookingAsync(today);

        // Assert
        var allocatedBookings = _dbContext.Bookings.ToList();
        Assert.Equal("Battery1", allocatedBookings[0].Battery.Name); // Should remain unchanged
        Assert.Equal("Boat1", allocatedBookings[0].Boat.Name);

        Assert.NotNull(allocatedBookings[1].Battery); // New allocation
        Assert.NotNull(allocatedBookings[1].Boat);
    }

    [Fact]
    public async Task AllocateDailyBookingAsync_ShouldSkipFutureBookings()
    {
        // Arrange
        var today = DateTime.Today;
        var futureDate = today.AddDays(1);

        var bookings = new List<Booking>
        {
            new Booking(today, "User1"),    // Today's booking
            new Booking(futureDate, "User2") // Future booking
        };
        var batteries = new List<Battery>
        {
            new Battery("Battery1"),
            new Battery("Battery2")
        };
        var boats = new List<Boat>
        {
            new Boat("Boat1"),
            new Boat("Boat2")
        };

        // Seed data
        await _dbContext.Bookings.AddRangeAsync(bookings);
        await _dbContext.Batteries.AddRangeAsync(batteries);
        await _dbContext.Boats.AddRangeAsync(boats);
        await _dbContext.SaveChangesAsync();

        // Act
        await _service.AllocateDailyBookingAsync(today);

        // Assert
        var allocatedBookings = _dbContext.Bookings.ToList();
        Assert.NotNull(allocatedBookings[0].Battery); // Today's booking should be allocated
        Assert.NotNull(allocatedBookings[0].Boat);

        Assert.Null(allocatedBookings[1].Battery); // Future booking should remain unallocated
        Assert.Null(allocatedBookings[1].Boat);
    }
}
