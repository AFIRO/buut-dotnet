using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using Rise.Server.Controllers;
using Rise.Services.Bookings;
using Rise.Shared.Bookings;
using Microsoft.Extensions.Options;
using Rise.Domain.Bookings;
using Rise.Server.Settings;
using Rise.Shared.Enums;
using Shouldly;
using Rise.Services.Events;
using Rise.Services.Events.Booking;

namespace Rise.Server.Tests.Controllers;

public class BookingControllerTest
{
    private readonly Mock<IBookingService> _mockBookingService;
    private readonly Mock<IEventDispatcher> _mockEventDispatcher;
    private readonly BookingController _controller;

    public BookingControllerTest()
    {
        _mockBookingService = new Mock<IBookingService>();
        _mockEventDispatcher = new Mock<IEventDispatcher>();

        var bookingSettings = Options.Create(new BookingSettings
        {
            MinReservationDays = 1,
            MaxReservationDays = 30
        });

        _controller = new BookingController(_mockBookingService.Object, bookingSettings, _mockEventDispatcher.Object);

        // Set up a fake user with authorization roles
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "auth0|12345"),
            new Claim(ClaimTypes.Role, "User")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task GetAllBookings_ReturnsOkResult_WithBookings()
    {
        // Arrange
        var bookings = new List<BookingDto.ViewBooking>
        {
            new BookingDto.ViewBooking { bookingId = "1", /* other properties */ },
            new BookingDto.ViewBooking { bookingId = "2", /* other properties */ }
        };

        _mockBookingService.Setup(s => s.GetAllAsync()).ReturnsAsync(bookings);

        // Act
        var result = await _controller.GetAllBookings();

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.ShouldNotBeNull();
        okResult.StatusCode.ShouldBe(StatusCodes.Status200OK);
        okResult.Value.ShouldBe(bookings);
    }

    [Fact]
    public async Task GetAllBookings_ReturnsInternalServerError_WhenExceptionIsThrown()
    {
        // Arrange
        _mockBookingService.Setup(s => s.GetAllAsync()).ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetAllBookings();

        // Assert
        var statusCodeResult = result.Result as ObjectResult;
        statusCodeResult.ShouldNotBeNull();
        statusCodeResult.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        statusCodeResult.Value.ShouldBe("An error occurred while processing your request.");
    }

    [Fact]
    public async Task Get_ReturnsOkResult_WithBooking_WhenBookingExists()
    {
        // Arrange
        var bookingId = "1";
        var booking = new BookingDto.ViewBooking { bookingId = bookingId };
        _mockBookingService.Setup(s => s.GetBookingById(bookingId)).ReturnsAsync(booking);

        // Act
        var result = await _controller.Get(bookingId);

        // Assert
        var okResult = result.Result as OkObjectResult;
        okResult.ShouldNotBeNull();
        okResult.StatusCode.ShouldBe(StatusCodes.Status200OK);
        okResult.Value.ShouldBe(booking);
    }

    [Fact]
    public async Task Get_ReturnsNotFound_WhenBookingDoesNotExist()
    {
        // Arrange
        var bookingId = "999"; // Non-existent ID
        _mockBookingService.Setup(s => s.GetBookingById(bookingId)).ReturnsAsync((BookingDto.ViewBooking?)null);

        // Act
        var result = await _controller.Get(bookingId);

        // Assert
        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult.ShouldNotBeNull();
        notFoundResult.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        notFoundResult.Value.ShouldBe($"Booking with ID '{bookingId}' was not found.");
    }

    [Fact]
    public async Task Get_ReturnsBadRequest_WhenBookingIdIsInvalid()
    {
        // Act
        var result = await _controller.Get("");

        // Assert
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult.ShouldNotBeNull();
        badRequestResult.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        badRequestResult.Value.ShouldBe("Booking ID cannot be null or empty.");
    }

    [Fact]
    public async Task Post_ReturnsCreatedAtActionResult_WhenBookingIsCreated()
    {
        // Arrange
        var booking = new BookingDto.NewBooking
        {
            /* properties */
        };
        var createdBooking = new BookingDto.ViewBooking { bookingId = "1" };

        _mockBookingService.Setup(s => s.CreateBookingAsync(booking)).ReturnsAsync(createdBooking);

        // Act
        var result = await _controller.Post(booking);

        // Assert
        var createdResult = result as CreatedAtActionResult;
        createdResult.ShouldNotBeNull();
        createdResult.StatusCode.ShouldBe(StatusCodes.Status201Created);
        createdResult.RouteValues["id"].ShouldBe(createdBooking.bookingId);
        createdResult.Value.ShouldBe(createdBooking);
    }

    [Fact]
    public async Task Post_ReturnsBadRequest_WhenBookingIsNull()
    {
        // Act
        var result = await _controller.Post(null);

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.ShouldNotBeNull();
        badRequestResult.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        badRequestResult.Value.ShouldBe("Booking details cannot be null.");
    }

    [Fact]
    public async Task Post_ReturnsInternalServerError_WhenExceptionIsThrown()
    {
        // Arrange
        var booking = new BookingDto.NewBooking
        {
            /* properties */
        };
        _mockBookingService.Setup(s => s.CreateBookingAsync(booking)).ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.Post(booking);

        // Assert
        var statusCodeResult = result as ObjectResult;
        statusCodeResult.ShouldNotBeNull();
        statusCodeResult.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        statusCodeResult.Value.ShouldBe("An error occurred while processing your request: Database error");
    }

    [Fact]
    public async Task Delete_ReturnsNoContent_WhenBookingIsDeleted()
    {
        // Arrange
        var bookingId = "1";
        var existingBooking = new BookingDto.ViewBooking { bookingId = bookingId, userId = "user1" };
        _mockBookingService.Setup(s => s.GetBookingById(bookingId)).ReturnsAsync(existingBooking);
        _mockBookingService.Setup(s => s.DeleteBookingAsync(bookingId)).ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(bookingId);

        // Assert
        var noContentResult = result as NoContentResult;
        noContentResult.ShouldNotBeNull();
        noContentResult.StatusCode.ShouldBe(StatusCodes.Status204NoContent);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenBookingDoesNotExist()
    {
        // Arrange
        var bookingId = "999"; // Non-existent ID
        _mockBookingService.Setup(s => s.DeleteBookingAsync(bookingId)).ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(bookingId);

        // Assert
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult.ShouldNotBeNull();
        notFoundResult.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        notFoundResult.Value.ShouldBe($"Booking with ID '{bookingId}' was not found.");
    }

    [Fact]
    public async Task Delete_ReturnsBadRequest_WhenBookingIdIsInvalid()
    {
        // Act
        var result = await _controller.Delete("");

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.ShouldNotBeNull();
        badRequestResult.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        badRequestResult.Value.ShouldBe("Booking ID cannot be null or empty.");
    }

    [Fact]
    public async Task Put_ReturnsNoContent_WhenBookingIsUpdated()
    {
        // Arrange
        var bookingId = "1";
        var existingBooking = new BookingDto.ViewBooking
        {
            bookingId = bookingId,
            bookingDate = DateTime.UtcNow,
            userId = "user1",
            timeSlot = TimeSlot.Morning
        };
        var updateBooking = new BookingDto.UpdateBooking
        {
            bookingId = bookingId,
            bookingDate = DateTime.UtcNow.AddDays(1)
        };


        _mockBookingService.Setup(s => s.GetBookingById(bookingId)).ReturnsAsync(existingBooking);
        _mockBookingService.Setup(s => s.UpdateBookingAsync(updateBooking)).ReturnsAsync(true);

        // Act
        var result = await _controller.Put(bookingId, updateBooking);

        // Assert
        var noContentResult = result as NoContentResult;
        noContentResult.ShouldNotBeNull();
        noContentResult.StatusCode.ShouldBe(StatusCodes.Status204NoContent);
    }

    [Fact]
    public async Task Put_ReturnsNotFound_WhenBookingDoesNotExist()
    {
        // Arrange
        var bookingId = "999"; // Non-existent ID
        var booking = new BookingDto.UpdateBooking { bookingId = bookingId };
        _mockBookingService.Setup(s => s.UpdateBookingAsync(booking)).ReturnsAsync(false);

        // Act
        var result = await _controller.Put(bookingId, booking);

        // Assert
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult.ShouldNotBeNull();
        notFoundResult.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        notFoundResult.Value.ShouldBe($"Booking with ID '{bookingId}' was not found.");
    }

    [Fact]
    public async Task Put_ReturnsBadRequest_WhenBookingIdIsInvalid()
    {
        // Arrange
        var booking = new BookingDto.UpdateBooking { bookingId = "1" };

        // Act
        var result = await _controller.Put("", booking);

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.ShouldNotBeNull();
        badRequestResult.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        badRequestResult.Value.ShouldBe("Invalid booking ID or details.");
    }

    [Fact]
    public async Task GetBookingsByDateRange_ReturnsOkResult_WithBookings()
    {
        // Arrange
        var startDate = DateTime.UtcNow.Date.AddDays(-1);
        var endDate = DateTime.UtcNow.Date.AddDays(1);
        var bookings = new List<BookingDto.ViewBookingCalender>
        {
            new BookingDto.ViewBookingCalender { BookingDate = startDate.AddDays(1), TimeSlot = TimeSlot.Morning },
            new BookingDto.ViewBookingCalender { BookingDate = startDate.AddDays(1), TimeSlot = TimeSlot.Afternoon }
        };

        _mockBookingService.Setup(s => s.GetTakenTimeslotsInDateRange(startDate, endDate)).ReturnsAsync(bookings);

        // Act
        var result = await _controller.GetBookingsByDateRange(startDate, endDate);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.ShouldNotBeNull();
        okResult.StatusCode.ShouldBe(StatusCodes.Status200OK);
        okResult.Value.ShouldBe(bookings);
    }

    [Fact]
    public async Task GetBookingsByDateRange_ReturnsBadRequest_WhenDatesAreMissing()
    {
        // Act
        var result = await _controller.GetBookingsByDateRange(null, null);

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.ShouldNotBeNull();
        badRequestResult.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        badRequestResult.Value.ShouldBe("Start date and end date are required.");
    }

    [Fact]
    public async Task GetBookingsByDateRange_ReturnsInternalServerError_WhenExceptionIsThrown()
    {
        // Arrange
        var startDate = DateTime.UtcNow.Date.AddDays(-1);
        var endDate = DateTime.UtcNow.Date.AddDays(1);

        _mockBookingService.Setup(s => s.GetTakenTimeslotsInDateRange(startDate, endDate))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetBookingsByDateRange(startDate, endDate);

        // Assert
        var statusCodeResult = result as ObjectResult;
        statusCodeResult.ShouldNotBeNull();
        statusCodeResult.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        statusCodeResult.Value.ShouldBe("An error occurred while processing your request.");
    }

    [Fact]
    public async Task GetFreeTimeslotsByDateRange_ReturnsOkResult_WithFreeTimeslots()
    {
        // Arrange
        var startDate = DateTime.UtcNow.Date.AddDays(1);
        var endDate = DateTime.UtcNow.Date.AddDays(2);
        var freeTimeslots = new List<BookingDto.ViewBookingCalender>
        {
            new BookingDto.ViewBookingCalender
                { BookingDate = DateTime.Now, Available = true, TimeSlot = TimeSlot.Morning },
            new BookingDto.ViewBookingCalender
                { BookingDate = DateTime.Now, Available = true, TimeSlot = TimeSlot.Afternoon }
        };

        _mockBookingService.Setup(s => s.GetFreeTimeslotsInDateRange(startDate, endDate)).ReturnsAsync(freeTimeslots);

        // Act
        var result = await _controller.GetFreeTimeslotsByDateRange(startDate, endDate);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.ShouldNotBeNull();
        okResult.StatusCode.ShouldBe(StatusCodes.Status200OK);
        okResult.Value.ShouldBe(freeTimeslots);
    }

    [Fact]
    public async Task GetFreeTimeslotsByDateRange_ReturnsBadRequest_WhenDatesAreMissing()
    {
        // Act
        var result = await _controller.GetFreeTimeslotsByDateRange(null, null);

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.ShouldNotBeNull();
        badRequestResult.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        badRequestResult.Value.ShouldBe("Start date and end date are required.");
    }

    [Fact]
    public async Task GetFreeTimeslotsByDateRange_ReturnsInternalServerError_WhenExceptionIsThrown()
    {
        // Arrange
        var startDate = DateTime.UtcNow.Date.AddDays(1);
        var endDate = DateTime.UtcNow.Date.AddDays(2);

        _mockBookingService.Setup(s => s.GetFreeTimeslotsInDateRange(startDate, endDate))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetFreeTimeslotsByDateRange(startDate, endDate);

        // Assert
        var statusCodeResult = result as ObjectResult;
        statusCodeResult.ShouldNotBeNull();
        statusCodeResult.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        statusCodeResult.Value.ShouldBe("An error occurred while processing your request.");
    }

    [Fact]
    public async Task GetAllUserBookings_ShouldReturnOkResult_WhenUserHasBookings()
    {
        // Arrange
        var userId = "1";
        var bookings = new List<BookingDto.ViewBooking> { new BookingDto.ViewBooking() { bookingId = "123" } };
        _mockBookingService.Setup(b => b.GetAllUserBookings(userId)).ReturnsAsync(bookings);

        // Act
        var result = await _controller.GetAllUserBookings(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(bookings, okResult.Value);
    }

    [Fact]
    public async Task GetAllUserBookings_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = "1";
        _mockBookingService.Setup(b => b.GetAllUserBookings(userId))
            .ThrowsAsync(new UserNotFoundException("User not found"));

        // Act
        var result = await _controller.GetAllUserBookings(userId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task GetFutureUserBookings_ShouldReturnFutureBookings_WhenUserExists()
    {
        // Arrange
        var userId = "1";
        var futureBooking = new Booking(DateTime.UtcNow.AddDays(1), userId)
        {
            Id = Guid.NewGuid().ToString(),
            IsDeleted = false
        };

        _mockBookingService.Setup(b => b.GetFutureUserBookings(userId)).ReturnsAsync(new List<BookingDto.ViewBooking>
        {
            new BookingDto.ViewBooking { bookingId = futureBooking.Id }
        });

        // Act
        var result = await _controller.GetFutureUserBookings(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var bookings = Assert.IsType<List<BookingDto.ViewBooking>>(okResult.Value);
        Assert.Single(bookings);
        Assert.Equal(futureBooking.Id, bookings.First().bookingId);
    }

    [Fact]
    public async Task GetFutureUserBookings_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = "nonexistent_user";
        _mockBookingService.Setup(b => b.GetFutureUserBookings(userId)).ThrowsAsync(new UserNotFoundException("error"));

        // Act
        var result = await _controller.GetFutureUserBookings(userId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task GetFutureUserBookings_ShouldReturnEmptyList_WhenNoFutureBookingsExist()
    {
        // Arrange
        var userId = "1";
        _mockBookingService.Setup(b => b.GetFutureUserBookings(userId))
            .ReturnsAsync(new List<BookingDto.ViewBooking>());

        // Act
        var result = await _controller.GetFutureUserBookings(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var bookings = Assert.IsType<List<BookingDto.ViewBooking>>(okResult.Value);
        Assert.Empty(bookings);
    }

    [Fact]
    public async Task GetFutureUserBookings_ShouldHandleUnexpectedException()
    {
        // Arrange
        var userId = "1";
        _mockBookingService.Setup(b => b.GetFutureUserBookings(userId)).ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _controller.GetFutureUserBookings(userId);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
    }

    [Fact]
    public async Task GetPastUserBookings_ShouldReturnPastBookings_WhenUserExists()
    {
        // Arrange
        var userId = "1";
        var pastBooking = new Booking(DateTime.UtcNow.AddDays(-1), userId)
        {
            Id = Guid.NewGuid().ToString(),
            IsDeleted = false
        };

        _mockBookingService.Setup(b => b.GetPastUserBookings(userId)).ReturnsAsync(new List<BookingDto.ViewBooking>
    {
        new BookingDto.ViewBooking { bookingId = pastBooking.Id }
    });

        // Act
        var result = await _controller.GetPastUserBookings(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var bookings = Assert.IsType<List<BookingDto.ViewBooking>>(okResult.Value);
        Assert.Single(bookings);
        Assert.Equal(pastBooking.Id, bookings.First().bookingId);
    }

    [Fact]
    public async Task GetPastUserBookings_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = "nonexistent_user";
        _mockBookingService.Setup(b => b.GetPastUserBookings(userId)).ThrowsAsync(new UserNotFoundException("error"));

        // Act
        var result = await _controller.GetPastUserBookings(userId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task GetPastUserBookings_ShouldReturnEmptyList_WhenNoPastBookingsExist()
    {
        // Arrange
        var userId = "1";
        _mockBookingService.Setup(b => b.GetPastUserBookings(userId)).ReturnsAsync(new List<BookingDto.ViewBooking>());

        // Act
        var result = await _controller.GetPastUserBookings(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var bookings = Assert.IsType<List<BookingDto.ViewBooking>>(okResult.Value);
        Assert.Empty(bookings);
    }

    [Fact]
    public async Task GetPastUserBookings_ShouldHandleUnexpectedException()
    {
        // Arrange
        var userId = "1";
        _mockBookingService.Setup(b => b.GetPastUserBookings(userId)).ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _controller.GetPastUserBookings(userId);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
    }

    [Fact]
    public async Task Post_DispatchesBookingCreatedEvent_WhenBookingIsCreated()
    {
        // Arrange
        var booking = new BookingDto.NewBooking
        {
            /* properties */
        };
        var createdBooking = new BookingDto.ViewBooking
        {
            bookingId = "1",
            userId = "user1",
            bookingDate = DateTime.UtcNow,
            timeSlot = TimeSlot.Morning
        };

        _mockBookingService.Setup(s => s.CreateBookingAsync(booking)).ReturnsAsync(createdBooking);

        // Act
        await _controller.Post(booking);

        // Assert
        _mockEventDispatcher.Verify(dispatcher => dispatcher.DispatchAsync(
            It.Is<BookingCreatedEvent>(e =>
                e.BookingId == createdBooking.bookingId &&
                e.UserId == createdBooking.userId &&
                e.BookingDate == createdBooking.bookingDate &&
                e.TimeSlot == createdBooking.timeSlot
            )),
            Times.Once);
    }

    [Fact]
    public async Task Post_DoesNotDispatchBookingCreatedEvent_WhenBookingCreationFails()
    {
        // Arrange
        var booking = new BookingDto.NewBooking
        {
            /* populate properties */
        };

        _mockBookingService.Setup(s => s.CreateBookingAsync(booking)).ReturnsAsync((BookingDto.ViewBooking?)null);

        // Act
        await _controller.Post(booking);

        // Assert
        _mockEventDispatcher.Verify(ed => ed.DispatchAsync(It.IsAny<IEvent>()), Times.Never); // Verify it was never called
    }


    [Fact]
    public async Task Put_DispatchesBookingUpdatedEvent_WhenBookingIsUpdated()
    {
        // Arrange
        var bookingId = "1";
        var existingBooking = new BookingDto.ViewBooking
        {
            bookingId = bookingId,
            userId = "user1",
            bookingDate = DateTime.UtcNow,
            timeSlot = TimeSlot.Morning
        };
        var updateBooking = new BookingDto.UpdateBooking
        {
            bookingId = bookingId,
            bookingDate = DateTime.UtcNow.AddDays(1)
        };

        _mockBookingService.Setup(s => s.GetBookingById(bookingId)).ReturnsAsync(existingBooking);
        _mockBookingService.Setup(s => s.UpdateBookingAsync(updateBooking)).ReturnsAsync(true);

        // Act
        await _controller.Put(bookingId, updateBooking);

        // Assert
        _mockEventDispatcher.Verify(dispatcher => dispatcher.DispatchAsync(
            It.Is<BookingUpdatedEvent>(e =>
                e.BookingId == bookingId &&
                e.UserId == existingBooking.userId &&
                e.OldBookingDate == existingBooking.bookingDate &&
                e.OldTimeSlot == existingBooking.timeSlot &&
                e.NewBookingDate == updateBooking.bookingDate &&
                e.NewTimeSlot == TimeSlotEnumExtensions.ToTimeSlot(updateBooking.bookingDate.Value.Hour)
            )),
            Times.Once);
    }

    [Fact]
    public async Task Put_DoesNotDispatchBookingUpdatedEvent_WhenBookingUpdateFails()
    {
        // Arrange
        var bookingId = "1";
        var updatedBooking = new BookingDto.UpdateBooking { bookingId = bookingId };

        _mockBookingService.Setup(s => s.UpdateBookingAsync(updatedBooking)).ReturnsAsync(false);

        // Act
        await _controller.Put(bookingId, updatedBooking);

        // Assert
        _mockEventDispatcher.Verify(ed => ed.DispatchAsync(It.IsAny<IEvent>()), Times.Never); // Verify it was never called
    }


    [Fact]
    public async Task Delete_DispatchesBookingDeletedEvent_WhenBookingIsDeleted()
    {
        // Arrange
        var bookingId = "1";
        var existingBooking = new BookingDto.ViewBooking
        {
            bookingId = bookingId,
            userId = "user1",
            bookingDate = DateTime.UtcNow,
            timeSlot = TimeSlot.Morning
        };

        _mockBookingService.Setup(s => s.GetBookingById(bookingId)).ReturnsAsync(existingBooking);
        _mockBookingService.Setup(s => s.DeleteBookingAsync(bookingId)).ReturnsAsync(true);

        // Act
        await _controller.Delete(bookingId);

        // Assert
        _mockEventDispatcher.Verify(dispatcher => dispatcher.DispatchAsync(
            It.Is<BookingDeletedEvent>(e =>
                e.BookingId == bookingId &&
                e.UserId == existingBooking.userId &&
                e.BookingDate == existingBooking.bookingDate &&
                e.TimeSlot == existingBooking.timeSlot
            )),
            Times.Once);
    }

    [Fact]
    public async Task Delete_DoesNotDispatchBookingDeletedEvent_WhenBookingDeletionFails()
    {
        // Arrange
        var bookingId = "1";

        _mockBookingService.Setup(s => s.GetBookingById(bookingId)).ReturnsAsync((BookingDto.ViewBooking?)null);
        _mockBookingService.Setup(s => s.DeleteBookingAsync(bookingId)).ReturnsAsync(false);

        // Act
        await _controller.Delete(bookingId);

        // Assert
        _mockEventDispatcher.Verify(ed => ed.DispatchAsync(It.IsAny<IEvent>()), Times.Never); // Verify it was never called
    }

}