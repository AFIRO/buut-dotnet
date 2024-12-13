using System.Net;
using System.Net.Http.Json;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Rise.Shared.Bookings;
using Rise.Persistence;
using Moq;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using Rise.Shared.Enums;
using System.Text.Json;
using System;
using System.Text.Json.Serialization;
using Rise.Domain.Bookings;
using Rise.Domain.Users;

namespace Rise.Server.Tests.E2E;

[Collection("IntegrationTests")]
public class BookingControllerE2ETests : BaseControllerE2ETests
{
    public BookingControllerE2ETests(CustomWebApplicationFactory<Program> factory) : base(factory)
    {
    }

    protected override void SeedData()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var address1 = new Address("Afrikalaan", "5");
        var address2 = new Address("Bataviabrug", "35");
        
        var normalUser1 = new User("auth0|user1", "Normal", "User1", "user@example.com", DateTime.UtcNow.AddYears(-26), address1, "+0987654320");
        normalUser1.Roles.Add(new Role(RolesEnum.User));
        
        var normalUser2 = new User("auth0|user2", "Normal", "User2", "user@example.com", DateTime.UtcNow.AddYears(-25), address2, "+0987654321");
        normalUser2.Roles.Add(new Role(RolesEnum.User));
        
        dbContext.Users.AddRange(normalUser1, normalUser2);

        var booking1 = new Booking("booking1", DateTime.UtcNow.AddDays(4), normalUser1.Id, TimeSlot.Afternoon);
        var booking2 = new Booking("booking2", DateTime.UtcNow.AddDays(2), normalUser2.Id, TimeSlot.Morning);
        var booking3 = new Booking("booking3",DateTime.UtcNow.AddDays(3), normalUser2.Id, TimeSlot.Morning);
        var pastbooking = new Booking("booking4", DateTime.UtcNow.AddDays(-4), normalUser1.Id, TimeSlot.Afternoon);

        
        dbContext.Bookings.AddRange(booking1, booking2, booking3, pastbooking);
        dbContext.SaveChanges();
    }

    [Fact]
    public async Task GetAllBookings_Should_Return_All_Bookings()
    {
        // Arrange
        var token = GenerateJwtToken("Normal", "User", "auth0|user1");
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/Booking");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        var jsonResponse = await response.Content.ReadAsStringAsync();
        var bookings = JsonSerializer.Deserialize<IEnumerable<BookingDto.ViewBooking>>(jsonResponse, JsonOptions);

        Assert.NotNull(bookings);
        Assert.Equal(4, bookings.Count()); // Check if exactly 4 bookings are returned
    }

    [Theory]
    [InlineData("auth0|user1", 2)]
    [InlineData("auth0|user2", 2)]
    public async Task GetAllBookingsFromUser_Should_Return_All_UserBookings(string userId, int expectedBookingCount)
    {
        // Arrange
        var token = GenerateJwtToken("Normal", "User", userId);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/Booking/user/{userId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        var jsonResponse = await response.Content.ReadAsStringAsync();
        var bookings = JsonSerializer.Deserialize<IEnumerable<BookingDto.ViewBooking>>(jsonResponse, JsonOptions);

        Assert.NotNull(bookings);
        Assert.Equal(expectedBookingCount, bookings.Count());
    }
    
    [Fact]
    public async Task GetBookingById_Should_Return_Booking()
    {
        // Arrange
        var token = GenerateJwtToken("Normal", "User", "auth0|user1");
        var bookingId = "booking1";

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/Booking/{bookingId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        var jsonResponse = await response.Content.ReadAsStringAsync();
        var booking = JsonSerializer.Deserialize<BookingDto.ViewBooking>(jsonResponse, JsonOptions);

        Assert.NotNull(booking);
    }

    [Fact]
    public async Task GetBookingByInvalidId_Should_Return_NotFound()
    {
        // Arrange
        var token = GenerateJwtToken("Normal", "User", "auth0|user1");
        var bookingId = "invalidId";

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/Booking/{bookingId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateBooking_Should_Return_Created()
    {
        // Arrange
        var token = GenerateJwtToken("Normal", "User", "auth0|user1");
        var newBooking = new BookingDto.NewBooking
        {
            bookingDate = DateTime.UtcNow.Date.AddDays(5).AddHours(10),
            userId = "auth0|user1"
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/Booking")
        {
            Content = JsonContent.Create(newBooking)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateBooking_WithInvalidUser_Should_Return_NotFound()
    {
        // Arrange
        var token = GenerateJwtToken("Normal", "User", "auth0|user1");
        var invalidBooking = new BookingDto.NewBooking
        {
            userId = "user", // Invalid user
            bookingDate = DateTime.UtcNow.Date.AddDays(5).AddHours(10),
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/Booking")
        {
            Content = JsonContent.Create(invalidBooking)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
    
    [Fact]
    public async Task CreateBooking_WithInvalidDate_Should_Return_BadRequest()
    {
        // Arrange
        var token = GenerateJwtToken("Normal", "User", "auth0|user1");
        var invalidBooking = new BookingDto.NewBooking
        {
            userId = "auth0|user1",
            bookingDate = DateTime.UtcNow.Date.AddDays(5), //Invalid date
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/Booking")
        {
            Content = JsonContent.Create(invalidBooking)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task UpdateBooking_Should_Return_NoContent()
    {
        // Arrange
        var token = GenerateJwtToken("Normal", "User", "auth0|user1");
        var bookingId = "booking1";
        var updateBooking = new BookingDto.UpdateBooking
        {
            bookingId = bookingId,
            bookingDate = DateTime.UtcNow.Date.AddDays(7).AddHours(10),
        };

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/Booking/{bookingId}")
        {
            Content = JsonContent.Create(updateBooking)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task UpdateBooking_WithInvalidId_Should_Return_NotFound()
    {
        // Arrange
        var token = GenerateJwtToken("Normal", "User", "auth0|user1");
        var invalidBookingId = "invalidId";
        var updateBooking = new BookingDto.UpdateBooking
        {
            bookingId = invalidBookingId,
            bookingDate = DateTime.UtcNow.AddDays(7),
        };

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/Booking/{invalidBookingId}")
        {
            Content = JsonContent.Create(updateBooking)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteBooking_Should_Return_NoContent()
    {
        // Arrange
        var token = GenerateJwtToken("Normal", "User", "auth0|user1");
        var bookingId = "booking1";

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/Booking/{bookingId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteBooking_WithInvalidId_Should_Return_NotFound()
    {
        // Arrange
        var token = GenerateJwtToken("Normal", "User", "auth0|user1");
        var invalidBookingId = "invalidId";

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/Booking/{invalidBookingId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
    
    [Fact]
    public async Task GetFirstFreeTimeSlot_Should_Return_AvailableTimeslot()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/Booking/free/first-timeslot");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        var jsonResponse = await response.Content.ReadAsStringAsync();
        var timeslot = JsonSerializer.Deserialize<BookingDto.ViewBookingCalender>(jsonResponse, JsonOptions);

        Assert.NotNull(timeslot);
    }
    
    [Theory]
    [InlineData(0, 7, 3)]   // Range covering all bookings (3 bookings)
    [InlineData(1, 3, 2)]   // Range covering 2 bookings
    [InlineData(3, 5, 2)]   // Range covering 2 booking
    [InlineData(4, 5, 1)]   // Range covering 2 booking
    [InlineData(6, 10, 0)]  // Range covering no bookings
    public async Task GetBookingsByDateRange_Should_Return_Bookings_In_Range(int startOffset, int endOffset, int expectedBookingCount)
    {
        // Arrange
        var token = GenerateJwtToken("Normal", "User", "auth0|user1");
        var startDate = DateTime.UtcNow.Date.AddDays(startOffset);
        var endDate = DateTime.UtcNow.Date.AddDays(endOffset);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/Booking/byDateRange?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        var jsonResponse = await response.Content.ReadAsStringAsync();
        var bookings = JsonSerializer.Deserialize<IEnumerable<BookingDto.ViewBooking>>(jsonResponse, JsonOptions);

        Assert.NotNull(bookings);
        Assert.Equal(expectedBookingCount, bookings.Count());
    }

    [Theory]
    [InlineData(0, 7, 13)]   // Starts counting from day 2 => 2 bookings already in this range. 5 days * 3 bookings = 15 bookings => 15 - 2 = 13
    [InlineData(1, 3, 2)]   // Starts counting from day 2 => 1 booking already in this range. 1 days * 3 bookings = 3 bookings => 3 - 1 = 2
    [InlineData(3, 5, 7)]   // 2 bookings already in this range. 3 days * 3 bookings = 9 bookings => 9 - 2 = 7
    [InlineData(4, 5, 5)]   // 1 booking already in this range. 2 days * 3 bookings = 6 bookings => 6 - 1 = 5
    [InlineData(6, 10, 15)]  // 0 bookings already in this range. 5 days * 3 bookings = 15 timeslots
    public async Task GetFreeTimeslotsByDateRange_Should_Return_FreeTimeslots_In_Range(int startOffset, int endOffset, int expectedFreeTimeslotCount)
    {
        // Arrange
        var token = GenerateJwtToken("Normal", "User", "auth0|user1");
        var startDate = DateTime.UtcNow.Date.AddDays(startOffset);
        var endDate = DateTime.UtcNow.Date.AddDays(endOffset);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/Booking/free/byDateRange?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        
        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        var jsonResponse = await response.Content.ReadAsStringAsync();
        var freeTimeslots = JsonSerializer.Deserialize<IEnumerable<BookingDto.ViewBooking>>(jsonResponse, JsonOptions);

        Assert.NotNull(freeTimeslots);
        Assert.Equal(expectedFreeTimeslotCount, freeTimeslots.Count());
    }

    [Fact]
    public async Task GetFutureUserBookings_Should_Return_FutureBookings()
    {
        // Arrange
        var userId = "auth0|user1";
        var token = GenerateJwtToken("Normal", "User", userId);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/Booking/user/{userId}/future");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        var jsonResponse = await response.Content.ReadAsStringAsync();
        var bookings = JsonSerializer.Deserialize<IEnumerable<BookingDto.ViewBooking>>(jsonResponse, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        });

        Assert.NotNull(bookings);
        Assert.NotEmpty(bookings);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task GetFutureUserBookings_WithNullOrEmptyUserId_Should_Return_NotFoun(string userId)
    {
        // Arrange
        var token = GenerateJwtToken("Normal", "User", "auth0|user1");

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/Booking/user/{userId}/future");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetFutureUserBookings_WithNonExistentUser_Should_Return_NotFound()
    {
        // Arrange
        var nonExistentUserId = "auth0|nonexistent";
        var token = GenerateJwtToken("Normal", "User", nonExistentUserId);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/Booking/user/{nonExistentUserId}/future");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var jsonResponse = await response.Content.ReadAsStringAsync();
        Assert.Contains($"User with ID {nonExistentUserId} was not found.", jsonResponse);
    }
    
    [Fact]
    public async Task GetPastUserBookings_Should_Return_PastBookings()
    {
        // Arrange
        var userId = "auth0|user1";
        var token = GenerateJwtToken("Normal", "User", userId);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/Booking/user/{userId}/past");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        var jsonResponse = await response.Content.ReadAsStringAsync();
        var bookings = JsonSerializer.Deserialize<IEnumerable<BookingDto.ViewBooking>>(jsonResponse, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        });

        Assert.NotNull(bookings);
        Assert.NotEmpty(bookings);
    }
    
    [Fact]
    public async Task GetEmptyPastUserBookings_Should_Return_EmptyList()
    {
        // Arrange
        var userId = "auth0|user2";
        var token = GenerateJwtToken("Normal", "User", userId);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/Booking/user/{userId}/past");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        var jsonResponse = await response.Content.ReadAsStringAsync();
        var bookings = JsonSerializer.Deserialize<IEnumerable<BookingDto.ViewBooking>>(jsonResponse, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        });

        Assert.NotNull(bookings);
        Assert.Empty(bookings);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task GetPastUserBookings_WithNullOrEmptyUserId_Should_Return_NotFound(string userId)
    {
        // Arrange
        var token = GenerateJwtToken("Normal", "User", "auth0|user1");

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/Booking/user/{userId}/past");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPastUserBookings_WithNonExistentUser_Should_Return_NotFound()
    {
        // Arrange
        var nonExistentUserId = "auth0|nonexistent";
        var token = GenerateJwtToken("Normal", "User", nonExistentUserId);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/Booking/user/{nonExistentUserId}/past");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var jsonResponse = await response.Content.ReadAsStringAsync();
        Assert.Contains($"User with ID {nonExistentUserId} was not found.", jsonResponse);
    }
}
