using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Rise.Domain.Bookings;
using Rise.Persistence;
using Rise.Server.Settings;
using Rise.Services.Bookings;
using Rise.Shared.Bookings;
using Rise.Shared.Enums;
using Rise.Services.Users;
using Rise.Shared.Services;
using Microsoft.Extensions.Logging;


namespace Rise.Services.Tests.Bookings;

public class BookingServiceTest
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IOptions<BookingSettings> _bookingSettings;
    private readonly BookingService _bookingService;

    private readonly Mock<IValidationService> _validationServiceMock;
    private readonly Mock<ILogger<BookingService>> _loggerMock;

    public BookingServiceTest()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _validationServiceMock = new Mock<IValidationService>();
        _loggerMock = new Mock<ILogger<BookingService>>();

        var bookingSettings = new BookingSettings
        {
            MaxBookingLimit = 1,
            MinReservationDays = 3,
            MaxReservationDays = 30
        };
        _bookingSettings = Options.Create(bookingSettings);

        _bookingService = new BookingService(_dbContext, _bookingSettings, _validationServiceMock.Object, _loggerMock.Object);
    }

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_ShouldReturnBookings_WhenBookingsExist()
    {
        // Arrange
        var booking = new Booking(DateTime.UtcNow.Date.AddDays(5).AddHours(10), "user1");
        booking.AddTimeSlot(TimeSlot.Morning);

        var boat = new Boat("Sea Explorer");
        var battery = new Battery("HighPower Battery");

        booking.AddBoat(boat);
        booking.AddBattery(battery);

        await _dbContext.Bookings.AddAsync(booking);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _bookingService.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);

        var viewBooking = result.First();
        Assert.Equal(booking.BookingDate.Date, viewBooking.bookingDate.Date);
        Assert.Equal(booking.Boat.Name, viewBooking.boat.name);
        Assert.Equal(booking.Battery.Name, viewBooking.battery.name);
    }

    #endregion

    #region CreateBookingAsync

    [Fact]
    public async Task CreateBookingAsync_ShouldCreateBookingWithBoatAndBattery_WhenDataIsValid()
    {
        // Arrange
        var newBooking = new BookingDto.NewBooking
        {
            bookingDate = DateTime.UtcNow.Date.AddDays(10).AddHours(10),
            userId = "user1"
        };
        var boat = new Boat("Sea Explorer");
        var battery = new Battery("HighPower Battery");

        // Mocking ValidationService methods
        _validationServiceMock
            .Setup(v => v.CheckUserExistsAsync(newBooking.userId))
            .ReturnsAsync(true);
        _validationServiceMock
            .Setup(v => v.BookingExists(newBooking.bookingDate))
            .ReturnsAsync(false);
        _validationServiceMock
            .Setup(v => v.CheckUserMaxBookings(newBooking.userId))
            .ReturnsAsync(false);

        // Act
        var result = await _bookingService.CreateBookingAsync(newBooking);

        // Fetch the created booking from the database
        var booking = await _dbContext.Bookings.FindAsync(result.bookingId);
        booking.AddBoat(boat);
        booking.AddBattery(battery);
        await _dbContext.SaveChangesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(newBooking.bookingDate.Date, result.bookingDate.Date);
        Assert.Equal(booking.GetTimeSlot(), TimeSlot.Morning);

        // Check if the assigned Boat and Battery match
        Assert.Equal(boat.Name, booking.Boat.Name);
        Assert.Equal(battery.Name, booking.Battery.Name);
    }


    [Fact]
    public async Task CreateBookingAsync_ShouldThrowException_WhenUserMaxBookingsExceeded()
    {
        // Arrange
        var newBooking = new BookingDto.NewBooking
        {
            bookingDate = DateTime.UtcNow.Date.AddDays(10).AddHours(10),
            userId = "user1"
        };

        // Mocking ValidationService methods
        _validationServiceMock
            .Setup(v => v.CheckUserExistsAsync(newBooking.userId))
            .ReturnsAsync(true);
        _validationServiceMock
            .Setup(v => v.BookingExists(newBooking.bookingDate))
            .ReturnsAsync(false);
        _validationServiceMock
            .Setup(v => v.CheckUserMaxBookings(newBooking.userId))
            .ReturnsAsync(true); // User has reached max bookings

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _bookingService.CreateBookingAsync(newBooking));
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldThrowException_WhenUserDoesNotExist()
    {
        // Arrange
        var newBooking = new BookingDto.NewBooking
        {
            bookingDate = DateTime.UtcNow.AddDays(10),
            userId = "user1"
        };

        // Mocking ValidationService methods
        _validationServiceMock
            .Setup(v => v.CheckUserExistsAsync(newBooking.userId))
            .ReturnsAsync(false); // User does not exist

        // Act & Assert
        await Assert.ThrowsAsync<UserNotFoundException>(
            () => _bookingService.CreateBookingAsync(newBooking));
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldThrowException_WhenBookingAlreadyExists()
    {
        // Arrange
        var newBooking = new BookingDto.NewBooking
        {
            bookingDate = DateTime.UtcNow.AddDays(10),
            userId = "user1"
        };

        // Mocking ValidationService methods
        _validationServiceMock
            .Setup(v => v.CheckUserExistsAsync(newBooking.userId))
            .ReturnsAsync(true);
        _validationServiceMock
            .Setup(v => v.BookingExists(newBooking.bookingDate))
            .ReturnsAsync(true); // Booking already exists for this date

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _bookingService.CreateBookingAsync(newBooking));
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldThrowException_WhenBookingDateIsInThePast()
    {
        // Arrange
        var newBooking = new BookingDto.NewBooking
        {
            bookingDate = DateTime.UtcNow.AddDays(-1), // Past date
            userId = "user1"
        };

        _validationServiceMock
            .Setup(v => v.CheckUserExistsAsync(newBooking.userId))
            .ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _bookingService.CreateBookingAsync(newBooking));
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldCreateBookingWithoutBoatOrBattery_WhenNoBoatAndBatteryProvided()
    {
        // Arrange
        var newBooking = new BookingDto.NewBooking
        {
            bookingDate = DateTime.UtcNow.Date.AddDays(10).AddHours(10),
            userId = "user1"
        };

        _validationServiceMock
            .Setup(v => v.CheckUserExistsAsync(newBooking.userId))
            .ReturnsAsync(true);
        _validationServiceMock
            .Setup(v => v.BookingExists(newBooking.bookingDate))
            .ReturnsAsync(false);
        _validationServiceMock
            .Setup(v => v.CheckUserMaxBookings(newBooking.userId))
            .ReturnsAsync(false);

        // Act
        var result = await _bookingService.CreateBookingAsync(newBooking);
        var booking = await _dbContext.Bookings.FindAsync(result.bookingId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(newBooking.bookingDate.Date, result.bookingDate.Date);
        Assert.Null(booking.Boat);
        Assert.Null(booking.Battery);
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldIncrementBoatAndBatteryBookingCount_WhenBoatAndBatteryAssigned()
    {
        // Arrange
        var newBooking = new BookingDto.NewBooking
        {
            bookingDate = DateTime.UtcNow.Date.AddDays(10).AddHours(10),
            userId = "user1"
        };
        var boat = new Boat("Sea Explorer");
        var battery = new Battery("HighPower Battery");

        _validationServiceMock
            .Setup(v => v.CheckUserExistsAsync(newBooking.userId))
            .ReturnsAsync(true);
        _validationServiceMock
            .Setup(v => v.BookingExists(newBooking.bookingDate))
            .ReturnsAsync(false);
        _validationServiceMock
            .Setup(v => v.CheckUserMaxBookings(newBooking.userId))
            .ReturnsAsync(false);

        // Act
        var result = await _bookingService.CreateBookingAsync(newBooking);
        var booking = await _dbContext.Bookings.FindAsync(result.bookingId);

        booking.AddBoat(boat);
        booking.AddBattery(battery);
        await _dbContext.SaveChangesAsync();

        // Assert
        Assert.Equal(1, boat.CountBookings);
        Assert.Equal(1, battery.CountBookings);
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldThrowException_WhenBoatOrBatteryAssignmentIsInvalid()
    {
        // Arrange
        var newBooking = new BookingDto.NewBooking
        {
            bookingDate = DateTime.UtcNow.Date.AddDays(10).AddHours(10),
            userId = "user1"
        };

        var invalidBoat = (Boat)null;

        // Mocking
        _validationServiceMock
            .Setup(v => v.CheckUserExistsAsync(newBooking.userId))
            .ReturnsAsync(true);

        var result = await _bookingService.CreateBookingAsync(newBooking);
        var booking = await _dbContext.Bookings.FindAsync(result.bookingId);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => booking.AddBoat(invalidBoat));
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldThrowException_WhenBookingDateIsNotAvailableForTimeSlot()
    {
        // Arrange
        var newBooking = new BookingDto.NewBooking
        {
            bookingDate = DateTime.UtcNow.AddDays(10).AddHours(10),
            userId = "user1",
        };

        _validationServiceMock
            .Setup(v => v.CheckUserExistsAsync(newBooking.userId))
            .ReturnsAsync(true);
        _validationServiceMock
            .Setup(v => v.BookingExists(It.IsAny<DateTime>()))
            .ReturnsAsync(true); // Simulate the time slot is booked

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _bookingService.CreateBookingAsync(newBooking));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task CreateBookingAsync_ShouldThrowException_WhenUserIdIsNullOrEmpty(string userId)
    {
        // Arrange
        var newBooking = new BookingDto.NewBooking
        {
            bookingDate = DateTime.UtcNow.AddDays(10),
            userId = userId
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _bookingService.CreateBookingAsync(newBooking));
    }

    [Fact]
    public async Task CreateBookingAsync_ShouldRollbackTransaction_WhenExceptionIsThrownDuringBooking()
    {
        // Arrange
        var newBooking = new BookingDto.NewBooking
        {
            bookingDate = DateTime.UtcNow.AddDays(10),
            userId = "user1"
        };

        _validationServiceMock
            .Setup(v => v.CheckUserExistsAsync(newBooking.userId))
            .ReturnsAsync(true);
        _validationServiceMock
            .Setup(v => v.BookingExists(It.IsAny<DateTime>()))
            .Throws(new Exception("Database error"));

        // Act
        await Assert.ThrowsAsync<Exception>(
            () => _bookingService.CreateBookingAsync(newBooking));

        // Assert
        var bookingCount = await _dbContext.Bookings.CountAsync();
        Assert.Equal(0, bookingCount); // Ensure no booking was added
    }

    [Fact]
    public async Task AddCommentToBoat_ShouldIncreaseCommentCount_WhenCommentIsAdded()
    {
        // Arrange
        var boat = new Boat("Test Boat");
        var booking = new Booking(DateTime.UtcNow.AddDays(3), "user1");
        booking.AddBoat(boat);

        // Act
        booking.Boat.AddComment("Great experience!");

        // Assert
        Assert.Single(boat.ListComments);
        Assert.Equal("Great experience!", boat.ListComments.First());
    }

    [Fact]
    public async Task AddCommentToBattery_ShouldIncreaseCommentCount_WhenCommentIsAdded()
    {
        // Arrange
        var battery = new Battery("Test Battery");
        var booking = new Booking(DateTime.UtcNow.AddDays(3), "user1");
        booking.AddBattery(battery);

        // Act
        booking.Battery.AddComment("Long-lasting power!");

        // Assert
        Assert.Single(battery.ListComments);
        Assert.Equal("Long-lasting power!", battery.ListComments.First());
    }

    [Fact]
    public async Task AddBookingCountToBoat_ShouldIncreaseBookingCount_WhenBookingAdded()
    {
        // Arrange
        var boat = new Boat("Booking Counter Test Boat");
        var booking = new Booking(DateTime.UtcNow.Date.AddDays(3).AddHours(10), "user1");
        booking.AddBoat(boat);

        // Assert
        Assert.Equal(1, boat.CountBookings);
    }

    [Fact]
    public async Task AddBookingCountToBattery_ShouldIncreaseBookingCount_WhenBookingAdded()
    {
        // Arrange
        var battery = new Battery("Booking Counter Test Battery");
        var booking = new Booking(DateTime.UtcNow.Date.AddDays(3).AddHours(10), "user1");
        booking.AddBattery(battery);

        // Assert
        Assert.Equal(1, battery.CountBookings);
    }

    #endregion

    #region GetBookingById

    [Fact]
    public async Task GetBookingById_ShouldReturnBooking_WhenBookingExists()
    {
        // Arrange
        var bookingId = Guid.NewGuid().ToString();
        var booking = new Booking(DateTime.UtcNow.AddDays(5), "user1")
        {
            Id = bookingId,
            IsDeleted = false
        };

        await _dbContext.Bookings.AddAsync(booking);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _bookingService.GetBookingById(bookingId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(bookingId, result.bookingId);
        Assert.Equal(booking.BookingDate, result.bookingDate);
    }

    [Fact]
    public async Task GetBookingById_ShouldReturnNull_WhenBookingDoesNotExist()
    {
        // Arrange
        var nonExistentBookingId = Guid.NewGuid().ToString();

        // Act
        var result = await _bookingService.GetBookingById(nonExistentBookingId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetBookingById_ShouldReturnNull_WhenBookingIsMarkedDeleted()
    {
        // Arrange
        var bookingId = Guid.NewGuid().ToString();
        var booking = new Booking(DateTime.UtcNow.AddDays(5), "user1")
        {
            Id = bookingId,
            IsDeleted = true // Mark the booking as deleted
        };

        await _dbContext.Bookings.AddAsync(booking);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _bookingService.GetBookingById(bookingId);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task GetBookingById_ShouldThrowArgumentException_WhenBookingIdIsNullOrEmpty(string bookingId)
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _bookingService.GetBookingById(bookingId));
        Assert.Equal("Booking ID cannot be null or empty. (Parameter 'bookingId')", exception.Message);
    }

    [Fact]
    public async Task GetBookingById_ShouldReturnBookingWithDetails_WhenBookingHasBatteryAndBoat()
    {
        // Arrange
        var bookingId = Guid.NewGuid().ToString();
        var boat = new Boat("Sea Explorer");
        var battery = new Battery("HighPower Battery");

        var booking = new Booking(DateTime.UtcNow.AddDays(5), "user1")
        {
            Id = bookingId,
            Boat = boat,
            Battery = battery,
            IsDeleted = false
        };

        await _dbContext.Bookings.AddAsync(booking);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _bookingService.GetBookingById(bookingId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(bookingId, result.bookingId);
        Assert.Equal(boat.Name, result.boat.name);
        Assert.Equal(battery.Name, result.battery.name);
    }

    [Fact]
    public async Task GetBookingById_ShouldReturnBooking_WhenBookingHasNullBoatOrBattery()
    {
        // Arrange
        var bookingId = Guid.NewGuid().ToString();

        var booking = new Booking(DateTime.UtcNow.AddDays(5), "user1")
        {
            Id = bookingId,
            Boat = null, // No boat assigned
            Battery = null, // No battery assigned
            IsDeleted = false
        };

        await _dbContext.Bookings.AddAsync(booking);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _bookingService.GetBookingById(bookingId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(bookingId, result.bookingId);
        Assert.Null(result.boat.name);
        Assert.Null(result.battery.name);
    }

    #endregion

    #region UpdateBookingAsync

    [Fact]
    public async Task UpdateBookingAsync_ShouldUpdateBookingBoatAndBatteryDetails()
    {
        // Arrange
        var booking = new Booking(DateTime.UtcNow.AddDays(5), "user1", TimeSlot.Morning);
        var boat = new Boat("Old Boat");
        var battery = new Battery("Old Battery");

        booking.AddBoat(boat);
        booking.AddBattery(battery);

        await _dbContext.Bookings.AddAsync(booking);
        await _dbContext.SaveChangesAsync();

        var newBoat = new Boat("New Boat");
        var newBattery = new Battery("New Battery");

        // Act
        booking.AddBoat(newBoat);
        booking.AddBattery(newBattery);
        await _dbContext.SaveChangesAsync();

        // Assert
        var updatedBooking = await _dbContext.Bookings.FindAsync(booking.Id);
        Assert.Equal("New Boat", updatedBooking.Boat.Name);
        Assert.Equal("New Battery", updatedBooking.Battery.Name);
    }

    [Fact]
    public async Task UpdateBookingAsync_ShouldUpdateBookingDate_WhenBookingExists()
    {
        // Arrange
        var bookingId = Guid.NewGuid().ToString();
        var originalDate = DateTime.UtcNow.AddDays(5);
        var newDate = DateTime.UtcNow.AddDays(10);

        var booking = new Booking(originalDate, "user1")
        {
            Id = bookingId
        };

        await _dbContext.Bookings.AddAsync(booking);
        await _dbContext.SaveChangesAsync();

        var updateBookingDto = new BookingDto.UpdateBooking
        {
            bookingId = bookingId,
            bookingDate = newDate
        };

        _validationServiceMock
            .Setup(v => v.BookingExists(newDate))
            .ReturnsAsync(false); // No conflicting booking

        // Act
        var result = await _bookingService.UpdateBookingAsync(updateBookingDto);

        // Assert
        Assert.True(result);
        var updatedBooking = await _dbContext.Bookings.FindAsync(bookingId);
        Assert.Equal(newDate, updatedBooking.BookingDate);
    }

    [Fact]
    public async Task UpdateBookingAsync_ShouldThrowException_WhenBookingDoesNotExist()
    {
        // Arrange
        var updateBookingDto = new BookingDto.UpdateBooking
        {
            bookingId = Guid.NewGuid().ToString(), // Non-existent booking ID
            bookingDate = DateTime.UtcNow.AddDays(10)
        };

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(
            () => _bookingService.UpdateBookingAsync(updateBookingDto));
    }

    [Fact]
    public async Task UpdateBookingAsync_ShouldNotChangeBookingDate_WhenNewDateIsNull()
    {
        // Arrange
        var bookingId = Guid.NewGuid().ToString();
        var originalDate = DateTime.UtcNow.AddDays(5);

        var booking = new Booking(originalDate, "user1")
        {
            Id = bookingId
        };

        await _dbContext.Bookings.AddAsync(booking);
        await _dbContext.SaveChangesAsync();

        var updateBookingDto = new BookingDto.UpdateBooking
        {
            bookingId = bookingId,
            bookingDate = null // No new date provided
        };

        // Act
        var result = await _bookingService.UpdateBookingAsync(updateBookingDto);

        // Assert
        Assert.True(result);
        var updatedBooking = await _dbContext.Bookings.FindAsync(bookingId);
        Assert.Equal(originalDate, updatedBooking.BookingDate);
    }

    [Fact]
    public async Task UpdateBookingAsync_ShouldNotUpdate_WhenNewDateIsSameAsExistingDate()
    {
        // Arrange
        var bookingId = Guid.NewGuid().ToString();
        var originalDate = DateTime.UtcNow.AddDays(5);

        var booking = new Booking(originalDate, "user1")
        {
            Id = bookingId
        };

        await _dbContext.Bookings.AddAsync(booking);
        await _dbContext.SaveChangesAsync();

        var updateBookingDto = new BookingDto.UpdateBooking
        {
            bookingId = bookingId,
            bookingDate = originalDate // Same as existing date
        };

        // Act
        var result = await _bookingService.UpdateBookingAsync(updateBookingDto);

        // Assert
        Assert.True(result);
        var updatedBooking = await _dbContext.Bookings.FindAsync(bookingId);
        Assert.Equal(originalDate, updatedBooking.BookingDate); // No change should occur
    }

    [Fact]
    public async Task UpdateBookingAsync_ShouldThrowException_WhenBookingDateConflicts()
    {
        // Arrange
        var bookingId = Guid.NewGuid().ToString();
        var originalDate = DateTime.UtcNow.AddDays(5);
        var conflictingDate = DateTime.UtcNow.AddDays(10);

        var booking = new Booking(originalDate, "user1")
        {
            Id = bookingId
        };

        await _dbContext.Bookings.AddAsync(booking);
        await _dbContext.SaveChangesAsync();

        var updateBookingDto = new BookingDto.UpdateBooking
        {
            bookingId = bookingId,
            bookingDate = conflictingDate
        };

        _validationServiceMock
            .Setup(v => v.BookingExists(conflictingDate))
            .ReturnsAsync(true); // Conflicting booking exists

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _bookingService.UpdateBookingAsync(updateBookingDto));
    }

    [Fact]
    public async Task UpdateBookingAsync_ShouldReturnTrue_WhenUpdatingBookingWithoutDateChange()
    {
        // Arrange
        var bookingId = Guid.NewGuid().ToString();
        var originalDate = DateTime.UtcNow.AddDays(5);

        var booking = new Booking(originalDate, "user1")
        {
            Id = bookingId
        };

        await _dbContext.Bookings.AddAsync(booking);
        await _dbContext.SaveChangesAsync();

        var updateBookingDto = new BookingDto.UpdateBooking
        {
            bookingId = bookingId,
            bookingDate = originalDate // No date change
        };

        // Act
        var result = await _bookingService.UpdateBookingAsync(updateBookingDto);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task UpdateBookingAsync_ShouldThrowException_WhenBookingDateIsInThePast()
    {
        // Arrange
        var bookingId = Guid.NewGuid().ToString();
        var originalDate = DateTime.UtcNow.AddDays(5);
        var pastDate = DateTime.UtcNow.AddDays(-1);

        var booking = new Booking(originalDate, "user1")
        {
            Id = bookingId
        };

        await _dbContext.Bookings.AddAsync(booking);
        await _dbContext.SaveChangesAsync();

        var updateBookingDto = new BookingDto.UpdateBooking
        {
            bookingId = bookingId,
            bookingDate = pastDate // Setting to a past date
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _bookingService.UpdateBookingAsync(updateBookingDto));
    }

    [Fact]
    public async Task UpdateBookingAsync_ShouldThrowException_WhenBookingDateIsBeforeThreeDaysFromToday()
    {
        // Arrange
        var bookingId = Guid.NewGuid().ToString();
        var originalDate = DateTime.UtcNow.AddDays(5);
        var invalidDate = DateTime.UtcNow.AddDays(2); // Date before the allowed range (+3 days)

        var booking = new Booking(originalDate, "user1")
        {
            Id = bookingId
        };

        await _dbContext.Bookings.AddAsync(booking);
        await _dbContext.SaveChangesAsync();

        var updateBookingDto = new BookingDto.UpdateBooking
        {
            bookingId = bookingId,
            bookingDate = invalidDate
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _bookingService.UpdateBookingAsync(updateBookingDto));
    }

    [Fact]
    public async Task UpdateBookingAsync_ShouldThrowException_WhenBookingDateIsAfterThirtyDaysFromToday()
    {
        // Arrange
        var bookingId = Guid.NewGuid().ToString();
        var originalDate = DateTime.UtcNow.AddDays(5);
        var invalidDate = DateTime.UtcNow.AddDays(31); // Date after the allowed range (+30 days)

        var booking = new Booking(originalDate, "user1")
        {
            Id = bookingId
        };

        await _dbContext.Bookings.AddAsync(booking);
        await _dbContext.SaveChangesAsync();

        var updateBookingDto = new BookingDto.UpdateBooking
        {
            bookingId = bookingId,
            bookingDate = invalidDate
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _bookingService.UpdateBookingAsync(updateBookingDto));
    }

    [Fact]
    public async Task UpdateBookingAsync_ShouldUpdateBooking_WhenBookingDateIsWithinAllowedRange()
    {
        // Arrange
        var bookingId = Guid.NewGuid().ToString();
        var originalDate = DateTime.UtcNow.AddDays(5);
        var validDate = DateTime.UtcNow.AddDays(10); // Date within the allowed range

        var booking = new Booking(originalDate, "user1")
        {
            Id = bookingId
        };

        await _dbContext.Bookings.AddAsync(booking);
        await _dbContext.SaveChangesAsync();

        var updateBookingDto = new BookingDto.UpdateBooking
        {
            bookingId = bookingId,
            bookingDate = validDate
        };

        // Act
        var result = await _bookingService.UpdateBookingAsync(updateBookingDto);

        // Assert
        Assert.True(result);
        var updatedBooking = await _dbContext.Bookings.FindAsync(bookingId);
        Assert.Equal(validDate, updatedBooking.BookingDate);
    }


    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task UpdateBookingAsync_ShouldThrowException_WhenBookingIdIsNullOrEmpty(string bookingId)
    {
        // Arrange
        var updateBookingDto = new BookingDto.UpdateBooking
        {
            bookingId = bookingId,
            bookingDate = DateTime.UtcNow.AddDays(5)
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _bookingService.UpdateBookingAsync(updateBookingDto));
    }

    [Fact]
    public async Task UpdateBookingAsync_ShouldNotChangeBooking_WhenOnlyBookingIdIsProvided()
    {
        // Arrange
        var bookingId = Guid.NewGuid().ToString();
        var originalDate = DateTime.UtcNow.AddDays(5);

        var booking = new Booking(originalDate, "user1")
        {
            Id = bookingId
        };

        await _dbContext.Bookings.AddAsync(booking);
        await _dbContext.SaveChangesAsync();

        var updateBookingDto = new BookingDto.UpdateBooking
        {
            bookingId = bookingId,
            bookingDate = null // No update to the date
        };

        // Act
        var result = await _bookingService.UpdateBookingAsync(updateBookingDto);

        // Assert
        Assert.True(result);
        var updatedBooking = await _dbContext.Bookings.FindAsync(bookingId);
        Assert.Equal(originalDate, updatedBooking.BookingDate);
    }

    [Fact]
    public async Task UpdateBookingAsync_ShouldHandleConcurrentUpdates()
    {
        // Arrange
        var bookingId = Guid.NewGuid().ToString();
        var originalDate = DateTime.UtcNow.AddDays(5);

        var booking = new Booking(originalDate, "user1")
        {
            Id = bookingId
        };

        await _dbContext.Bookings.AddAsync(booking);
        await _dbContext.SaveChangesAsync();

        var updateBookingDto1 = new BookingDto.UpdateBooking
        {
            bookingId = bookingId,
            bookingDate = originalDate.AddDays(1)
        };

        var updateBookingDto2 = new BookingDto.UpdateBooking
        {
            bookingId = bookingId,
            bookingDate = originalDate.AddDays(2)
        };

        // Act
        var updateTask1 = _bookingService.UpdateBookingAsync(updateBookingDto1);
        var updateTask2 = _bookingService.UpdateBookingAsync(updateBookingDto2);

        await Task.WhenAll(updateTask1, updateTask2);

        // Assert
        var updatedBooking = await _dbContext.Bookings.FindAsync(bookingId);
        Assert.Contains(updatedBooking.BookingDate,
            new[] { updateBookingDto1.bookingDate.Value, updateBookingDto2.bookingDate.Value });
    }

    #endregion

    #region DeleteBookingAsync

    [Fact]
    public async Task DeleteBookingAsync_ShouldDeleteBooking_WhenBookingExists()
    {
        // Arrange
        var bookingId = Guid.NewGuid().ToString();
        var booking = new Booking(DateTime.UtcNow.AddDays(5), "user1")
        {
            Id = bookingId,
            IsDeleted = false
        };

        await _dbContext.Bookings.AddAsync(booking);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _bookingService.DeleteBookingAsync(bookingId);

        // Assert
        Assert.True(result);
        var deletedBooking = await _dbContext.Bookings.FindAsync(bookingId);
        Assert.True(deletedBooking.IsDeleted);
    }

    [Fact]
    public async Task DeleteBookingAsync_ShouldThrowException_WhenBookingDoesNotExist()
    {
        // Arrange
        var nonExistentBookingId = Guid.NewGuid().ToString();

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(
            () => _bookingService.DeleteBookingAsync(nonExistentBookingId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task DeleteBookingAsync_ShouldThrowException_WhenBookingIdIsNullOrEmpty(string bookingId)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _bookingService.DeleteBookingAsync(bookingId));
    }

    [Fact]
    public async Task DeleteBookingAsync_ShouldReturnTrue_WhenBookingIsAlreadyDeleted()
    {
        // Arrange
        var bookingId = Guid.NewGuid().ToString();
        var booking = new Booking(DateTime.UtcNow.AddDays(5), "user1")
        {
            Id = bookingId,
            IsDeleted = true // Booking is already marked as deleted
        };

        await _dbContext.Bookings.AddAsync(booking);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _bookingService.DeleteBookingAsync(bookingId);

        // Assert
        Assert.True(result);
        var deletedBooking = await _dbContext.Bookings.FindAsync(bookingId);
        Assert.True(deletedBooking.IsDeleted);
    }

    [Fact]
    public async Task DeleteBookingAsync_ShouldHandleConcurrentDeletes()
    {
        // Arrange
        var bookingId = Guid.NewGuid().ToString();
        var booking = new Booking(DateTime.UtcNow.AddDays(5), "user1")
        {
            Id = bookingId,
            IsDeleted = false
        };

        await _dbContext.Bookings.AddAsync(booking);
        await _dbContext.SaveChangesAsync();

        // Act
        var deleteTask1 = _bookingService.DeleteBookingAsync(bookingId);
        var deleteTask2 = _bookingService.DeleteBookingAsync(bookingId);

        await Task.WhenAll(deleteTask1, deleteTask2);

        // Assert
        var deletedBooking = await _dbContext.Bookings.FindAsync(bookingId);
        Assert.True(deletedBooking.IsDeleted);
    }

    [Fact]
    public async Task DeleteBookingAsync_ShouldOnlyModifyIsDeletedField()
    {
        // Arrange
        var bookingId = Guid.NewGuid().ToString();
        var booking = new Booking(DateTime.UtcNow.Date.AddDays(5), "user1")
        {
            Id = bookingId,
            IsDeleted = false,
            BookingDate = DateTime.UtcNow.Date.AddDays(5)
        };

        await _dbContext.Bookings.AddAsync(booking);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _bookingService.DeleteBookingAsync(bookingId);

        // Assert
        Assert.True(result);
        var deletedBooking = await _dbContext.Bookings.FindAsync(bookingId);
        Assert.True(deletedBooking.IsDeleted);
        Assert.Equal(DateTime.UtcNow.Date.AddDays(5), deletedBooking.BookingDate);
    }

    #endregion

    #region GetAllUserBookings

    [Fact]
    public async Task GetAllUserBookings_ShouldReturnBookings_WhenUserExistsAndHasBookings()
    {
        // Arrange
        var userId = "user1";
        var booking1 = new Booking(DateTime.UtcNow.AddDays(5), userId)
        {
            Id = Guid.NewGuid().ToString(),
            IsDeleted = false
        };
        var booking2 = new Booking(DateTime.UtcNow.AddDays(10), userId)
        {
            Id = Guid.NewGuid().ToString(),
            IsDeleted = false
        };

        await _dbContext.Bookings.AddAsync(booking1);
        await _dbContext.Bookings.AddAsync(booking2);
        await _dbContext.SaveChangesAsync();

        _validationServiceMock.Setup(v => v.CheckUserExistsAsync(userId)).ReturnsAsync(true);

        // Act
        var result = await _bookingService.GetAllUserBookings(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Contains(result, b => b.bookingId == booking1.Id);
        Assert.Contains(result, b => b.bookingId == booking2.Id);
    }

    [Fact]
    public async Task GetAllUserBookings_ShouldReturnEmptyList_WhenUserExistsAndHasNoBookings()
    {
        // Arrange
        var userId = "user1";

        _validationServiceMock.Setup(v => v.CheckUserExistsAsync(userId)).ReturnsAsync(true);

        // Act
        var result = await _bookingService.GetAllUserBookings(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllUserBookings_ShouldThrowUserNotFoundException_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = "nonexistentUser";

        _validationServiceMock.Setup(v => v.CheckUserExistsAsync(userId)).ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<UserNotFoundException>(
            () => _bookingService.GetAllUserBookings(userId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task GetAllUserBookings_ShouldThrowArgumentException_WhenUserIdIsNullOrEmpty(string userId)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _bookingService.GetAllUserBookings(userId));
    }

    [Fact]
    public async Task GetAllUserBookings_ShouldReturnBookingsWithRoles()
    {
        // Arrange
        
        // Clear existing bookings
        _dbContext.Bookings.RemoveRange(_dbContext.Bookings);
        await _dbContext.SaveChangesAsync();
        
        var userId = "user1";
        var openBooking = new Booking(DateTime.UtcNow.AddDays(5), userId)
        {
            Id = Guid.NewGuid().ToString(),
            IsDeleted = false
        };
        var deletedBooking = new Booking(DateTime.UtcNow.AddDays(10), userId)
        {
            Id = Guid.NewGuid().ToString(),
            IsDeleted = true // Soft deleted booking
        };
        
        await _dbContext.Bookings.AddAsync(openBooking);
        await _dbContext.Bookings.AddAsync(deletedBooking);
        await _dbContext.SaveChangesAsync();

        _validationServiceMock.Setup(v => v.CheckUserExistsAsync(userId)).ReturnsAsync(true);

        // Act
        var result = await _bookingService.GetAllUserBookings(userId);

        // Assert
        Assert.NotNull(result);
        // Assert that openBooking has status OPEN
        var openBookingResult = result.FirstOrDefault(b => b.bookingId == openBooking.Id);
        Assert.NotNull(openBookingResult);
        Assert.Equal(BookingStatus.OPEN, openBookingResult.status);

        // Assert that deletedBooking has status DELETED
        var deletedBookingResult = result.FirstOrDefault(b => b.bookingId == deletedBooking.Id);
        Assert.NotNull(deletedBookingResult);
        Assert.Equal(BookingStatus.CANCELED, deletedBookingResult.status);
    }

    [Fact]
    public async Task GetAllUserBookings_ShouldReturnBookings_WhenBoatOrBatteryIsNull()
    {
        // Arrange
        var userId = "user1";
        var booking = new Booking(DateTime.UtcNow.AddDays(5), userId)
        {
            Id = Guid.NewGuid().ToString(),
            IsDeleted = false,
            Boat = null, // Boat is not assigned
            Battery = null // Battery is not assigned
        };

        await _dbContext.Bookings.AddAsync(booking);
        await _dbContext.SaveChangesAsync();

        _validationServiceMock.Setup(v => v.CheckUserExistsAsync(userId)).ReturnsAsync(true);

        // Act
        var result = await _bookingService.GetAllUserBookings(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Null(result.First().boat.name);
        Assert.Null(result.First().battery.name);
    }

    [Fact]
    public async Task GetAllUserBookings_ShouldReturnBookings_InDescendingOrderOfBookingDate()
    {
        // Arrange
        var userId = "user1";
        var booking1 = new Booking(DateTime.UtcNow.AddDays(5), userId)
        {
            Id = Guid.NewGuid().ToString(),
            IsDeleted = false
        };
        var booking2 = new Booking(DateTime.UtcNow.AddDays(10), userId)
        {
            Id = Guid.NewGuid().ToString(),
            IsDeleted = false
        };

        await _dbContext.Bookings.AddAsync(booking1);
        await _dbContext.Bookings.AddAsync(booking2);
        await _dbContext.SaveChangesAsync();

        _validationServiceMock.Setup(v => v.CheckUserExistsAsync(userId)).ReturnsAsync(true);

        // Act
        var result = await _bookingService.GetAllUserBookings(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Equal(booking2.Id, result.First().bookingId); // Booking with later date should come first
    }

    [Fact]
    public async Task GetAllUserBookings_ShouldReturnBookings_WhenUserHasManyBookings()
    {
        // Arrange
        var userId = "user1";
        for (int i = 0; i < 1000; i++)
        {
            var booking = new Booking(DateTime.UtcNow.AddDays(i + 1), userId)
            {
                Id = Guid.NewGuid().ToString(),
                IsDeleted = false
            };
            await _dbContext.Bookings.AddAsync(booking);
        }

        await _dbContext.SaveChangesAsync();

        _validationServiceMock.Setup(v => v.CheckUserExistsAsync(userId)).ReturnsAsync(true);

        // Act
        var result = await _bookingService.GetAllUserBookings(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1000, result.Count());
    }

    [Fact]
    public async Task GetAllUserBookings_ShouldHandleConcurrentRequests()
    {
        // Arrange
        var userId = "user1";
        var booking = new Booking(DateTime.UtcNow.AddDays(5), userId)
        {
            Id = Guid.NewGuid().ToString(),
            IsDeleted = false
        };

        await _dbContext.Bookings.AddAsync(booking);
        await _dbContext.SaveChangesAsync();

        _validationServiceMock.Setup(v => v.CheckUserExistsAsync(userId)).ReturnsAsync(true);

        // Act
        var task1 = _bookingService.GetAllUserBookings(userId);
        var task2 = _bookingService.GetAllUserBookings(userId);

        var results = await Task.WhenAll(task1, task2);

        // Assert
        Assert.All(results, result =>
        {
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(booking.Id, result.First().bookingId);
        });
    }

    [Fact]
    public async Task GetAllUserBookings_ShouldReturnOnlyUserBookings_WhenMultipleUsersHaveBookings()
    {
        // Arrange
        var userId = "user1";
        var otherUserId = "user2";

        var userBooking1 = new Booking(DateTime.UtcNow.AddDays(5), userId)
        {
            Id = Guid.NewGuid().ToString(),
            IsDeleted = false
        };
        var userBooking2 = new Booking(DateTime.UtcNow.AddDays(-5), userId)
        {
            Id = Guid.NewGuid().ToString(),
            IsDeleted = false
        };
        var otherUserBooking = new Booking(DateTime.UtcNow.AddDays(10), otherUserId)
        {
            Id = Guid.NewGuid().ToString(),
            IsDeleted = false
        };

        await _dbContext.Bookings.AddAsync(userBooking1);
        await _dbContext.Bookings.AddAsync(userBooking2);
        await _dbContext.Bookings.AddAsync(otherUserBooking);
        await _dbContext.SaveChangesAsync();

        _validationServiceMock.Setup(v => v.CheckUserExistsAsync(userId)).ReturnsAsync(true);

        // Act
        var result = await _bookingService.GetAllUserBookings(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Contains(result, b => b.bookingId == userBooking1.Id);
        Assert.Contains(result, b => b.bookingId == userBooking2.Id);
        Assert.DoesNotContain(result, b => b.bookingId == otherUserBooking.Id);
    }

    #endregion

    #region GetFutureUserBookings

    [Fact]
    public async Task GetFutureUserBookings_ShouldReturnFutureBookings_WhenUserExistsAndHasFutureBookings()
    {
        // Arrange
        var userId = "user1";
        var futureBooking1 = new Booking(DateTime.UtcNow.AddDays(5), userId)
        {
            Id = Guid.NewGuid().ToString(),
            IsDeleted = false
        };
        var futureBooking2 = new Booking(DateTime.UtcNow.AddDays(10), userId)
        {
            Id = Guid.NewGuid().ToString(),
            IsDeleted = false
        };

        await _dbContext.Bookings.AddAsync(futureBooking1);
        await _dbContext.Bookings.AddAsync(futureBooking2);
        await _dbContext.SaveChangesAsync();

        _validationServiceMock.Setup(v => v.CheckUserExistsAsync(userId)).ReturnsAsync(true);

        // Act
        var result = await _bookingService.GetFutureUserBookings(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Contains(result, b => b.bookingId == futureBooking1.Id);
        Assert.Contains(result, b => b.bookingId == futureBooking2.Id);
    }

    [Fact]
    public async Task GetFutureUserBookings_ShouldReturnEmptyList_WhenUserExistsButHasNoFutureBookings()
    {
        // Arrange
        var userId = "user1";

        _validationServiceMock.Setup(v => v.CheckUserExistsAsync(userId)).ReturnsAsync(true);

        // Act
        var result = await _bookingService.GetFutureUserBookings(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetFutureUserBookings_ShouldThrowUserNotFoundException_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = "nonexistentUser";

        _validationServiceMock.Setup(v => v.CheckUserExistsAsync(userId)).ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<UserNotFoundException>(
            () => _bookingService.GetFutureUserBookings(userId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task GetFutureUserBookings_ShouldThrowArgumentException_WhenUserIdIsNullOrEmpty(string userId)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _bookingService.GetFutureUserBookings(userId));
    }

    [Fact]
    public async Task GetFutureUserBookings_ShouldNotReturnPastBookings()
    {
        // Arrange
        var userId = "user1";
        var pastBooking = new Booking(DateTime.UtcNow.AddDays(-5), userId)
        {
            Id = Guid.NewGuid().ToString(),
            IsDeleted = false
        };
        var futureBooking = new Booking(DateTime.UtcNow.AddDays(5), userId)
        {
            Id = Guid.NewGuid().ToString(),
            IsDeleted = false
        };

        await _dbContext.Bookings.AddAsync(pastBooking);
        await _dbContext.Bookings.AddAsync(futureBooking);
        await _dbContext.SaveChangesAsync();

        _validationServiceMock.Setup(v => v.CheckUserExistsAsync(userId)).ReturnsAsync(true);

        // Act
        var result = await _bookingService.GetFutureUserBookings(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(futureBooking.Id, result.First().bookingId);
    }

    [Fact]
    public async Task GetFutureUserBookings_ShouldReturnBookings_InDescendingOrderOfBookingDate()
    {
        // Arrange
        var userId = "user1";
        var futureBooking1 = new Booking(DateTime.UtcNow.AddDays(5), userId)
        {
            Id = Guid.NewGuid().ToString(),
            IsDeleted = false
        };
        var futureBooking2 = new Booking(DateTime.UtcNow.AddDays(10), userId)
        {
            Id = Guid.NewGuid().ToString(),
            IsDeleted = false
        };

        await _dbContext.Bookings.AddAsync(futureBooking1);
        await _dbContext.Bookings.AddAsync(futureBooking2);
        await _dbContext.SaveChangesAsync();

        _validationServiceMock.Setup(v => v.CheckUserExistsAsync(userId)).ReturnsAsync(true);

        // Act
        var result = await _bookingService.GetFutureUserBookings(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Equal(futureBooking2.Id, result.First().bookingId); // Booking with later date should come first
    }

    [Fact]
    public async Task GetFutureUserBookings_ShouldNotReturnDeletedFutureBookings()
    {
        // Arrange
        var userId = "user1";
        var futureBooking = new Booking(DateTime.UtcNow.AddDays(5), userId)
        {
            Id = Guid.NewGuid().ToString(),
            IsDeleted = true // Marked as deleted
        };

        await _dbContext.Bookings.AddAsync(futureBooking);
        await _dbContext.SaveChangesAsync();

        _validationServiceMock.Setup(v => v.CheckUserExistsAsync(userId)).ReturnsAsync(true);

        // Act
        var result = await _bookingService.GetFutureUserBookings(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetFutureUserBookings_ShouldReturnOnlyFutureBookings_WhenUserHasPastAndFutureBookings()
    {
        // Arrange
        var userId = "user1";
        var pastBooking = new Booking(DateTime.UtcNow.AddDays(-5), userId)
        {
            Id = Guid.NewGuid().ToString(),
            IsDeleted = false
        };
        var futureBooking = new Booking(DateTime.UtcNow.AddDays(5), userId)
        {
            Id = Guid.NewGuid().ToString(),
            IsDeleted = false
        };

        await _dbContext.Bookings.AddAsync(pastBooking);
        await _dbContext.Bookings.AddAsync(futureBooking);
        await _dbContext.SaveChangesAsync();

        _validationServiceMock.Setup(v => v.CheckUserExistsAsync(userId)).ReturnsAsync(true);

        // Act
        var result = await _bookingService.GetFutureUserBookings(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(futureBooking.Id, result.First().bookingId);
    }

    [Fact]
    public async Task GetFutureUserBookings_ShouldIncludeBookingsOnCurrentDateTime()
    {
        // Arrange
        var userId = "user1";
        var currentBooking = new Booking(DateTime.UtcNow.Date.AddDays(1), userId) // Move booking to tomorrow
        {
            Id = Guid.NewGuid().ToString(),
            IsDeleted = false
        };

        await _dbContext.Bookings.AddAsync(currentBooking);
        await _dbContext.SaveChangesAsync();

        _validationServiceMock.Setup(v => v.CheckUserExistsAsync(userId)).ReturnsAsync(true);

        // Act
        var result = await _bookingService.GetFutureUserBookings(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(currentBooking.Id, result.First().bookingId);
    }


    [Fact]
    public async Task GetFutureUserBookings_ShouldReturnAllFutureBookings_WhenUserHasManyFutureBookings()
    {
        // Arrange
        var userId = "user1";
        var futureBookings = new List<Booking>();

        for (int i = 0; i < 1000; i++)
        {
            var futureBooking = new Booking(DateTime.UtcNow.AddDays(i + 1), userId)
            {
                Id = Guid.NewGuid().ToString(),
                IsDeleted = false
            };
            futureBookings.Add(futureBooking);
        }

        await _dbContext.Bookings.AddRangeAsync(futureBookings);
        await _dbContext.SaveChangesAsync();

        _validationServiceMock.Setup(v => v.CheckUserExistsAsync(userId)).ReturnsAsync(true);

        // Act
        var result = await _bookingService.GetFutureUserBookings(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1000, result.Count());
    }

    [Fact]
    public async Task GetFutureUserBookings_ShouldHandleConcurrentRequestsCorrectly()
    {
        // Arrange
        var userId = "user1";
        var futureBooking = new Booking(DateTime.UtcNow.AddDays(5), userId)
        {
            Id = Guid.NewGuid().ToString(),
            IsDeleted = false
        };

        await _dbContext.Bookings.AddAsync(futureBooking);
        await _dbContext.SaveChangesAsync();

        _validationServiceMock.Setup(v => v.CheckUserExistsAsync(userId)).ReturnsAsync(true);

        // Act
        var task1 = _bookingService.GetFutureUserBookings(userId);
        var task2 = _bookingService.GetFutureUserBookings(userId);

        var results = await Task.WhenAll(task1, task2);

        // Assert
        Assert.All(results, result =>
        {
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(futureBooking.Id, result.First().bookingId);
        });
    }

    [Fact]
    public async Task GetFutureUserBookings_ShouldReturnOnlyUserBookings_WhenMultipleUsersHaveFutureBookings()
    {
        // Arrange
        var userId = "user1";
        var otherUserId = "user2";

        var userBooking = new Booking(DateTime.UtcNow.AddDays(5), userId)
        {
            Id = Guid.NewGuid().ToString(),
            IsDeleted = false
        };
        var otherUserBooking = new Booking(DateTime.UtcNow.AddDays(10), otherUserId)
        {
            Id = Guid.NewGuid().ToString(),
            IsDeleted = false
        };

        await _dbContext.Bookings.AddAsync(userBooking);
        await _dbContext.Bookings.AddAsync(otherUserBooking);
        await _dbContext.SaveChangesAsync();

        _validationServiceMock.Setup(v => v.CheckUserExistsAsync(userId)).ReturnsAsync(true);

        // Act
        var result = await _bookingService.GetFutureUserBookings(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(userBooking.Id, result.First().bookingId);
    }

    #endregion

    #region GetPastUserBookings

    [Fact]
    public async Task GetPastUserBookings_ShouldReturnPastBookings_WhenUserExistsAndHasPastBookings()
    {
        // Arrange
        var userId = "user1";
        var pastBooking1 = new Booking(DateTime.UtcNow.AddDays(-5), userId)
        {
            Id = Guid.NewGuid().ToString(),
            IsDeleted = false
        };
        var pastBooking2 = new Booking(DateTime.UtcNow.AddDays(-10), userId)
        {
            Id = Guid.NewGuid().ToString(),
            IsDeleted = false
        };

        await _dbContext.Bookings.AddAsync(pastBooking1);
        await _dbContext.Bookings.AddAsync(pastBooking2);
        await _dbContext.SaveChangesAsync();

        _validationServiceMock.Setup(v => v.CheckUserExistsAsync(userId)).ReturnsAsync(true);

        // Act
        var result = await _bookingService.GetPastUserBookings(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Contains(result, b => b.bookingId == pastBooking1.Id);
        Assert.Contains(result, b => b.bookingId == pastBooking2.Id);
    }

    [Fact]
    public async Task GetPastUserBookings_ShouldReturnEmptyList_WhenUserExistsButHasNoPastBookings()
    {
        // Arrange
        var userId = "user1";

        _validationServiceMock.Setup(v => v.CheckUserExistsAsync(userId)).ReturnsAsync(true);

        // Act
        var result = await _bookingService.GetPastUserBookings(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPastUserBookings_ShouldThrowUserNotFoundException_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = "nonexistentUser";

        _validationServiceMock.Setup(v => v.CheckUserExistsAsync(userId)).ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<UserNotFoundException>(
            () => _bookingService.GetPastUserBookings(userId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task GetPastUserBookings_ShouldThrowArgumentException_WhenUserIdIsNullOrEmpty(string userId)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _bookingService.GetPastUserBookings(userId));
    }

    [Fact]
    public async Task GetPastUserBookings_ShouldNotReturnFutureBookings()
    {
        // Arrange
        var userId = "user1";
        var pastBooking = new Booking(DateTime.UtcNow.AddDays(-5), userId)
        {
            Id = Guid.NewGuid().ToString(),
            IsDeleted = false
        };
        var futureBooking = new Booking(DateTime.UtcNow.AddDays(5), userId)
        {
            Id = Guid.NewGuid().ToString(),
            IsDeleted = false
        };

        await _dbContext.Bookings.AddAsync(pastBooking);
        await _dbContext.Bookings.AddAsync(futureBooking);
        await _dbContext.SaveChangesAsync();

        _validationServiceMock.Setup(v => v.CheckUserExistsAsync(userId)).ReturnsAsync(true);

        // Act
        var result = await _bookingService.GetPastUserBookings(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(pastBooking.Id, result.First().bookingId);
    }

    [Fact]
    public async Task GetPastUserBookings_ShouldNotReturnDeletedPastBookings()
    {
        // Arrange
        var userId = "user1";
        var pastBooking = new Booking(DateTime.UtcNow.AddDays(-5), userId)
        {
            Id = Guid.NewGuid().ToString(),
            IsDeleted = true // Marked as deleted
        };

        await _dbContext.Bookings.AddAsync(pastBooking);
        await _dbContext.SaveChangesAsync();

        _validationServiceMock.Setup(v => v.CheckUserExistsAsync(userId)).ReturnsAsync(true);

        // Act
        var result = await _bookingService.GetPastUserBookings(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetPastUserBookings_ShouldReturnBookings_InDescendingOrderOfBookingDate()
    {
        // Arrange
        var userId = "user1";
        var pastBooking1 = new Booking(DateTime.UtcNow.AddDays(-10), userId)
        {
            Id = Guid.NewGuid().ToString(),
            IsDeleted = false
        };
        var pastBooking2 = new Booking(DateTime.UtcNow.AddDays(-5), userId)
        {
            Id = Guid.NewGuid().ToString(),
            IsDeleted = false
        };

        await _dbContext.Bookings.AddAsync(pastBooking1);
        await _dbContext.Bookings.AddAsync(pastBooking2);
        await _dbContext.SaveChangesAsync();

        _validationServiceMock.Setup(v => v.CheckUserExistsAsync(userId)).ReturnsAsync(true);

        // Act
        var result = await _bookingService.GetPastUserBookings(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Equal(pastBooking2.Id, result.First().bookingId); // Booking with later date should come first
    }

    [Fact]
    public async Task GetPastUserBookings_ShouldHandleConcurrentRequestsCorrectly()
    {
        // Arrange
        var userId = "user1";
        var pastBooking = new Booking(DateTime.UtcNow.AddDays(-5), userId)
        {
            Id = Guid.NewGuid().ToString(),
            IsDeleted = false
        };

        await _dbContext.Bookings.AddAsync(pastBooking);
        await _dbContext.SaveChangesAsync();

        _validationServiceMock.Setup(v => v.CheckUserExistsAsync(userId)).ReturnsAsync(true);

        // Act
        var task1 = _bookingService.GetPastUserBookings(userId);
        var task2 = _bookingService.GetPastUserBookings(userId);

        var results = await Task.WhenAll(task1, task2);

        // Assert
        Assert.All(results, result =>
        {
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(pastBooking.Id, result.First().bookingId);
        });
    }

    [Fact]
    public async Task GetPastUserBookings_ShouldReturnAllPastBookings_WhenUserHasManyPastBookings()
    {
        // Arrange
        var userId = "user1";
        var pastBookings = new List<Booking>();

        for (int i = 0; i < 1000; i++)
        {
            var pastBooking = new Booking(DateTime.UtcNow.AddDays(-i - 1), userId)
            {
                Id = Guid.NewGuid().ToString(),
                IsDeleted = false
            };
            pastBookings.Add(pastBooking);
        }

        await _dbContext.Bookings.AddRangeAsync(pastBookings);
        await _dbContext.SaveChangesAsync();

        _validationServiceMock.Setup(v => v.CheckUserExistsAsync(userId)).ReturnsAsync(true);

        // Act
        var result = await _bookingService.GetPastUserBookings(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1000, result.Count());
    }
    
    [Fact]
    public async Task GetPastUserBookings_ShouldReturnOnlyUserBookings_WhenMultipleUsersHavePastBookings()
    {
        // Arrange
        var userId = "user1";
        var otherUserId = "user2";

        var userBooking = new Booking(DateTime.UtcNow.AddDays(-5), userId)
        {
            Id = Guid.NewGuid().ToString(),
            IsDeleted = false
        };
        var otherUserBooking = new Booking(DateTime.UtcNow.AddDays(-10), otherUserId)
        {
            Id = Guid.NewGuid().ToString(),
            IsDeleted = false
        };

        await _dbContext.Bookings.AddAsync(userBooking);
        await _dbContext.Bookings.AddAsync(otherUserBooking);
        await _dbContext.SaveChangesAsync();

        _validationServiceMock.Setup(v => v.CheckUserExistsAsync(userId)).ReturnsAsync(true);

        // Act
        var result = await _bookingService.GetPastUserBookings(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(userBooking.Id, result.First().bookingId);
    }


    #endregion

    #region GetTakenTimeslotsInDateRange

    [Fact]
    public async Task GetTakenTimeslotsInDateRange_NullStartDate_ThrowsArgumentNullException()
    {
        DateTime? endDate = DateTime.Now.AddDays(10);
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _bookingService.GetTakenTimeslotsInDateRange(null, endDate));
    }

    [Fact]
    public async Task GetTakenTimeslotsInDateRange_NullEndDate_ThrowsArgumentNullException()
    {
        DateTime? startDate = DateTime.Now.AddDays(10);
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _bookingService.GetTakenTimeslotsInDateRange(startDate, null));
    }

    [Fact]
    public async Task GetTakenTimeslotsInDateRange_StartDateAfterEndDate_ThrowsArgumentException()
    {
        DateTime? startDate = DateTime.Now.AddDays(1);
        DateTime? endDate = DateTime.Now;
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _bookingService.GetTakenTimeslotsInDateRange(startDate, endDate));
    }

    [Fact]
    public async Task GetTakenTimeslotsInDateRange_StartDateEqualToEndDate_ThrowsArgumentException()
    {
        DateTime startDate = DateTime.Now.Date;
        DateTime endDate = startDate;

        // Add bookings that should return to the in-memory database
        _dbContext.Bookings.Add(new Booking(startDate, "1", TimeSlot.Morning));
        _dbContext.Bookings.Add(new Booking(startDate, "1", TimeSlot.Afternoon));
        // Add bookings that should not return
        _dbContext.Bookings.Add(new Booking(startDate.AddDays(+1), "1", TimeSlot.Evening));
        _dbContext.Bookings.Add(new Booking(startDate.AddDays(-1), "1", TimeSlot.Morning));

        await _dbContext.SaveChangesAsync();

        var result = await _bookingService.GetTakenTimeslotsInDateRange(startDate, endDate);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Contains(result, b => b.BookingDate.Date == startDate.Date && b.TimeSlot == TimeSlot.Morning);
        Assert.Contains(result, b => b.BookingDate.Date == startDate.Date && b.TimeSlot == TimeSlot.Afternoon);
    }

    [Fact]
    public async Task GetTakenTimeslotsInDateRange_NoBookings_ReturnsEmptyList()
    {
        DateTime startDate = DateTime.Now;
        DateTime endDate = DateTime.Now.AddDays(5);

        var result = await _bookingService.GetTakenTimeslotsInDateRange(startDate, endDate);

        Assert.Empty(result);
    }


    [Fact]
    public async Task GetTakenTimeslotsInDateRange_WithBookings_ReturnsCorrectTimeslots()
    {
        const int END_DATE_EXTRA_DAYS = 5;
        DateTime startDate = DateTime.Now.Date;
        DateTime endDate = DateTime.Now.Date.AddDays(END_DATE_EXTRA_DAYS);

        // Add bookings that should return to the in-memory database
        _dbContext.Bookings.Add(new Booking(startDate.AddDays(END_DATE_EXTRA_DAYS - 1), "1", TimeSlot.Morning));
        _dbContext.Bookings.Add(new Booking(startDate.AddDays(END_DATE_EXTRA_DAYS - 2), "1", TimeSlot.Afternoon));
        // Add bookings that should not return
        _dbContext.Bookings.Add(new Booking(startDate.AddDays(END_DATE_EXTRA_DAYS + 1), "1", TimeSlot.Evening));
        _dbContext.Bookings.Add(new Booking(startDate.AddDays(END_DATE_EXTRA_DAYS + 5), "1", TimeSlot.Morning));


        await _dbContext.SaveChangesAsync();

        var result = await _bookingService.GetTakenTimeslotsInDateRange(startDate, endDate);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Contains(result,
            b => b.BookingDate.Date == startDate.Date.AddDays(END_DATE_EXTRA_DAYS - 1) &&
                 b.TimeSlot == TimeSlot.Morning);
        Assert.Contains(result,
            b => b.BookingDate.Date == startDate.Date.AddDays(END_DATE_EXTRA_DAYS - 2) &&
                 b.TimeSlot == TimeSlot.Afternoon);
    }

    #endregion

    #region GetFreeTimeslotsInDateRange

    [Fact]
    public async Task GetFreeTimeslotsInDateRange_NullStartDate_ThrowsArgumentNullException()
    {
        DateTime? endDate = DateTime.Now.AddDays(3);
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _bookingService.GetFreeTimeslotsInDateRange(null, endDate));
    }

    [Fact]
    public async Task GetFreeTimeslotsInDateRange_NullEndDate_ThrowsArgumentNullException()
    {
        DateTime? startDate = DateTime.Now;
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _bookingService.GetFreeTimeslotsInDateRange(startDate, null));
    }

    [Fact]
    public async Task GetFreeTimeslotsInDateRange_StartDateAfterEndDate_ThrowsArgumentException()
    {
        DateTime? startDate = DateTime.Now.AddDays(5);
        DateTime? endDate = DateTime.Now;
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _bookingService.GetFreeTimeslotsInDateRange(startDate, endDate));
    }

    [Fact]
    public async Task GetFreeTimeslotsInDateRange_StartDateEqualToEndDate_ReturnsCorrectTimeslots()
    {
        DateTime
            startDate = DateTime.Now.Date.AddDays(3); //Only free slots available between +3 and +30 days from today
        DateTime endDate = startDate;

        // Add bookings that should return to the in-memory database
        _dbContext.Bookings.Add(new Booking(startDate, "1", TimeSlot.Morning));
        _dbContext.Bookings.Add(new Booking(startDate, "1", TimeSlot.Afternoon));
        // Add bookings that should not return
        _dbContext.Bookings.Add(new Booking(startDate.AddDays(+1), "1", TimeSlot.Evening));
        _dbContext.Bookings.Add(new Booking(startDate.AddDays(-1), "1", TimeSlot.Morning));

        var booking = new BookingDto.ViewBookingCalender
        {
            BookingDate = startDate,
            TimeSlot = TimeSlot.Evening,
            Available = true
        };

        await _dbContext.SaveChangesAsync();

        var result = await _bookingService.GetFreeTimeslotsInDateRange(startDate, endDate);

        Assert.NotNull(result);
        Assert.Equal(1, result.Count());
        Assert.Equivalent(result.First(), booking);
    }

    [Fact]
    public async Task GetFreeTimeslotsInDateRange_EndDateWithinNonBookablePeriod_ReturnsEmptyList()
    {
        DateTime startDate = DateTime.Now;
        DateTime endDate = DateTime.Now.AddDays(2); // ToDo -> if we move the blockdate to somewhere else get it there

        // Add bookings that should not get return to the in-memory database
        _dbContext.Bookings.Add(new Booking(startDate.AddDays(1), "1", TimeSlot.Morning));
        _dbContext.Bookings.Add(new Booking(startDate.AddDays(2), "1", TimeSlot.Afternoon));

        var result = await _bookingService.GetFreeTimeslotsInDateRange(startDate, endDate);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetFreeTimeslotsInDateRange_WithBookings_ReturnsAvailableTimeslots()
    {
        DateTime startDate = DateTime.Now;
        DateTime endDate = DateTime.Now.AddDays(5);

        _dbContext.Bookings.Add(new Booking(DateTime.Now.AddDays(3), "1", TimeSlot.Morning));
        _dbContext.Bookings.Add(new Booking(DateTime.Now.AddDays(4), "1", TimeSlot.Afternoon));

        var result = await _bookingService.GetFreeTimeslotsInDateRange(startDate, endDate);

        Assert.NotNull(result);
        Assert.DoesNotContain(result, r => r.BookingDate == DateTime.Now.AddDays(3) && r.TimeSlot == TimeSlot.Morning);
        Assert.DoesNotContain(result,
            r => r.BookingDate == DateTime.Now.AddDays(4) && r.TimeSlot == TimeSlot.Afternoon);
    }

    #endregion
}