using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Rise.Domain.Bookings;
using Rise.Domain.Users;
using Rise.Persistence;
using Rise.Server.Settings;
using Rise.Shared.Bookings;
using Rise.Shared.Services;

namespace Rise.Shared;

public class ValidationServiceTest
{
    private readonly ValidationService _validationService;
    private readonly ApplicationDbContext _dbContext;

    public ValidationServiceTest()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);

        var bookingSettings = Options.Create(new BookingSettings { MaxBookingLimit = 5 });
        _validationService = new ValidationService(_dbContext, bookingSettings);
    }

    [Fact]
    public async Task CheckUserExistsAsync_ShouldReturnTrue_WhenUserExists()
    {
        // Arrange
        var user = new User("user1", "John", "Doe", "john.doe@example.com", DateTime.UtcNow.AddYears(-30),
            new Address("Afrikalaan", "5"), "1234567890");
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _validationService.CheckUserExistsAsync(user.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CheckUserExistsAsync_ShouldReturnFalse_WhenUserDoesNotExist()
    {
        // Arrange
        var nonExistentUserId = "nonexistentUser";

        // Act
        var result = await _validationService.CheckUserExistsAsync(nonExistentUserId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task BookingExists_ShouldReturnTrue_WhenBookingExistsForDate()
    {
        // Arrange
        var bookingDate = DateTime.UtcNow.AddDays(5);
        var booking = new Booking(bookingDate, "user1");
        await _dbContext.Bookings.AddAsync(booking);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _validationService.BookingExists(bookingDate);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task BookingExists_ShouldReturnFalse_WhenBookingDoesNotExistForDate()
    {
        // Arrange
        var bookingDate = DateTime.UtcNow.AddDays(5);

        // Act
        var result = await _validationService.BookingExists(bookingDate);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CheckUserMaxBookings_ShouldReturnTrue_WhenUserReachesMaxBookings()
    {
        // Arrange
        var userId = "user1";

        for (int i = 0; i < 5; i++) // Assuming max booking limit is 5
        {
            var booking = new Booking(DateTime.UtcNow.AddDays(i + 1), userId);
            await _dbContext.Bookings.AddAsync(booking);
        }

        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _validationService.CheckUserMaxBookings(userId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CheckUserMaxBookings_ShouldReturnFalse_WhenUserHasLessThanMaxBookings()
    {
        // Arrange
        var userId = "user1";

        for (int i = 0; i < 3; i++) // Less than max booking limit
        {
            var booking = new Booking(DateTime.UtcNow.AddDays(i + 1), userId);
            await _dbContext.Bookings.AddAsync(booking);
        }

        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _validationService.CheckUserMaxBookings(userId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateBookingAsync_ShouldThrowArgumentException_WhenUserDoesNotExist()
    {
        // Arrange
        var nonExistentUserId = "nonexistentUser";
        var bookingDto = new BookingDto.UpdateBooking
            { bookingId = "booking1", bookingDate = DateTime.UtcNow.AddDays(10) };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _validationService.ValidateBookingAsync(nonExistentUserId, bookingDto));
    }

    [Fact]
    public async Task ValidateBookingAsync_ShouldReturnTrue_WhenUserExistsAndValidationPasses()
    {
        // Arrange
        var user = new User("user1", "John", "Doe", "john.doe@example.com", DateTime.UtcNow.AddYears(-30),
            new Address("Afrikalaan", "5"), "1234567890");
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();
        var bookingDto = new BookingDto.UpdateBooking
            { bookingId = "booking1", bookingDate = DateTime.UtcNow.AddDays(10) };

        // Act
        var result = await _validationService.ValidateBookingAsync(user.Id, bookingDto);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CheckUserExistsAsync_ShouldReturnFalse_WhenUserIsSoftDeleted()
    {
        // Arrange
        var user = new User("user1", "John", "Doe", "john.doe@example.com", DateTime.UtcNow.AddYears(-30),
            new Address("Afrikalaan", "5"), "1234567890")
        {
            IsDeleted = true
        };
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _validationService.CheckUserExistsAsync(user.Id);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task BookingExists_ShouldReturnFalse_WhenBookingIsSoftDeleted()
    {
        // Arrange
        var bookingDate = DateTime.UtcNow.AddDays(5);
        var booking = new Booking(bookingDate, "user1")
        {
            IsDeleted = true
        };
        await _dbContext.Bookings.AddAsync(booking);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _validationService.BookingExists(bookingDate);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task BookingExists_ShouldReturnTrue_WhenBookingExistsAtStartOfDay()
    {
        // Arrange
        var bookingDate = DateTime.UtcNow.Date; // Midnight of the current day
        var booking = new Booking(bookingDate, "user1");
        await _dbContext.Bookings.AddAsync(booking);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _validationService.BookingExists(bookingDate);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CheckUserMaxBookings_ShouldReturnFalse_WhenUserHasOneLessThanMaxBookings()
    {
        // Arrange
        var userId = "user1";

        for (int i = 0; i < 4; i++) // One less than the max limit of 5
        {
            var booking = new Booking(DateTime.UtcNow.AddDays(i + 1), userId);
            await _dbContext.Bookings.AddAsync(booking);
        }

        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _validationService.CheckUserMaxBookings(userId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CheckUserMaxBookings_ShouldReturnTrue_WhenUserHasExactlyMaxBookings()
    {
        // Arrange
        var userId = "user1";

        for (int i = 0; i < 5; i++) // Exactly the max limit of 5
        {
            var booking = new Booking(DateTime.UtcNow.AddDays(i + 1), userId);
            await _dbContext.Bookings.AddAsync(booking);
        }

        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _validationService.CheckUserMaxBookings(userId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateBookingAsync_ShouldReturnTrue_WhenAllConditionsAreMet()
    {
        // Arrange
        var user = new User("user1", "John", "Doe", "john.doe@example.com", DateTime.UtcNow.AddYears(-30),
            new Address("Afrikalaan", "5"), "1234567890");
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        var bookingDto = new BookingDto.UpdateBooking
            { bookingId = "booking1", bookingDate = DateTime.UtcNow.AddDays(10) };

        // Act
        var result = await _validationService.ValidateBookingAsync(user.Id, bookingDto);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateBookingAsync_ShouldThrowUserNotFoundException_WhenUserIsSoftDeleted()
    {
        // Arrange
        var user = new User("user1", "John", "Doe", "john.doe@example.com", DateTime.UtcNow.AddYears(-30), new Address("Afrikalaan", "5"), "1234567890")
        {
            IsDeleted = true // The user is soft deleted
        };
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        var bookingDto = new BookingDto.UpdateBooking { bookingId = "booking1", bookingDate = DateTime.UtcNow.AddDays(10) };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _validationService.ValidateBookingAsync(user.Id, bookingDto));
    }
    
        [Fact]
    public async Task CheckActiveBookings_ShouldReturnTrue_WhenUserHasActiveBookings()
    {
        // Arrange
        var userId = "user1";
        var user = new User(userId, "John", "Doe", "john.doe@example.com", DateTime.UtcNow.AddYears(-30),
            new Address("Afrikalaan", "5"), "1234567890");
        await _dbContext.Users.AddAsync(user);
        var booking = new Booking(DateTime.UtcNow.AddDays(5), userId);
        await _dbContext.Bookings.AddAsync(booking);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _validationService.CheckActiveBookings(userId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CheckActiveBookings_ShouldReturnFalse_WhenUserHasNoActiveBookings()
    {
        // Arrange
        var userId = "user1";
        var user = new User(userId, "John", "Doe", "john.doe@example.com", DateTime.UtcNow.AddYears(-30),
            new Address("Afrikalaan", "5"), "1234567890");
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _validationService.CheckActiveBookings(userId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CheckActiveBookings_ShouldThrowArgumentException_WhenUserDoesNotExist()
    {
        // Arrange
        var nonExistentUserId = "nonexistentUser";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _validationService.CheckActiveBookings(nonExistentUserId));
    }

    [Fact]
    public async Task CheckActiveBookings_ShouldReturnFalse_WhenBookingsAreSoftDeleted()
    {
        // Arrange
        var userId = "user1";
        var user = new User(userId, "John", "Doe", "john.doe@example.com", DateTime.UtcNow.AddYears(-30),
            new Address("Afrikalaan", "5"), "1234567890");
        await _dbContext.Users.AddAsync(user);
        var booking = new Booking(DateTime.UtcNow.AddDays(5), userId)
        {
            IsDeleted = true
        };
        await _dbContext.Bookings.AddAsync(booking);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _validationService.CheckActiveBookings(userId);

        // Assert
        Assert.False(result);
    }

}