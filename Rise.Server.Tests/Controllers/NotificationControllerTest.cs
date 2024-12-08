using Microsoft.AspNetCore.Mvc;
using Moq;
using Rise.Server.Controllers;
using Rise.Shared.Enums;
using Rise.Shared.Notifications;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Logging;

public class NotificationControllerTests
{
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly NotificationController _controller;
    private readonly Mock<ILogger<NotificationController>> _mockLogger;

    public NotificationControllerTests()
    {
        _mockNotificationService = new Mock<INotificationService>();
        _mockLogger = new Mock<ILogger<NotificationController>>();
        _controller = new NotificationController(_mockNotificationService.Object, _mockLogger.Object);
    }

    /// <summary>
    /// Helper method to create sample notifications.
    /// </summary>
    private List<NotificationDto.ViewNotification> CreateSampleNotifications()
    {
        return new List<NotificationDto.ViewNotification>
        {
            new NotificationDto.ViewNotification
            {
                NotificationId = "1",
                Title = "Sample Notification 1",
                Message = "Sample Message 1",
                IsRead = false,
                Type = NotificationType.General
            },
            new NotificationDto.ViewNotification
            {
                NotificationId = "2",
                Title = "Sample Notification 2",
                Message = "Sample Message 2",
                IsRead = true,
                Type = NotificationType.Alert
            }
        };
    }

    [Fact]
    public async Task GetAllNotifications_ReturnsOkResult_WithNotifications()
    {
        // Arrange
        var notifications = CreateSampleNotifications();
        _mockNotificationService.Setup(service => service.GetAllNotificationsAsync(It.IsAny<string>())).ReturnsAsync(notifications);

        // Act
        var result = await _controller.GetAllNotifications();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(notifications, okResult.Value);
    }

    [Fact]
    public async Task GetAllNotifications_ReturnsInternalServerError_OnException()
    {
        // Arrange
        _mockNotificationService.Setup(service => service.GetAllNotificationsAsync(It.IsAny<string>())).ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _controller.GetAllNotifications();

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task GetUserNotifications_ReturnsOkResult_WithUserNotifications()
    {
        // Arrange
        var userId = "123";
        var notifications = CreateSampleNotifications();
        _mockNotificationService.Setup(service => service.GetAllUserNotifications(userId, It.IsAny<string>())).ReturnsAsync(notifications);

        // Act
        var result = await _controller.GetUserNotifications(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(notifications, okResult.Value);
    }

    [Fact]
    public async Task GetUserNotifications_ReturnsBadRequest_OnArgumentNullException()
    {
        // Arrange
        var userId = "123";
        _mockNotificationService.Setup(service => service.GetAllUserNotifications(userId, It.IsAny<string>())).ThrowsAsync(new ArgumentNullException("userId"));

        // Act
        var result = await _controller.GetUserNotifications(userId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task CreateNotification_ReturnsOkResult_WithCreatedNotification()
    {
        // Arrange
        var newNotification = new NotificationDto.NewNotification
        {
            UserId = "123",
            Title_EN = "Title",
            Message_EN = "Message",
            Type = NotificationType.General
        };
        var createdNotification = new NotificationDto.ViewNotification
        {
            NotificationId = "1",
            Title = "Title",
            Message = "Message",
            IsRead = false,
            Type = NotificationType.General
        };
        _mockNotificationService.Setup(service => service.CreateNotificationAsync(newNotification, It.IsAny<string>(), It.IsAny<bool>()))
        .ReturnsAsync(createdNotification);

        // Act
        var result = await _controller.CreateNotification(newNotification);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(createdNotification, okResult.Value);
    }

    [Fact]
    public async Task CreateNotification_ReturnsBadRequest_OnArgumentNullException()
    {
        // Arrange
        var newNotification = new NotificationDto.NewNotification();
        _mockNotificationService.Setup(service => service.CreateNotificationAsync(newNotification, It.IsAny<string>(), It.IsAny<bool>()))
        .ThrowsAsync(new ArgumentNullException("notification"));
        
        // Act
        var result = await _controller.CreateNotification(newNotification);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task DeleteNotification_ReturnsOkResult_WhenNotificationIsDeleted()
    {
        // Arrange
        var notificationId = "1";
        _mockNotificationService.Setup(service => service.DeleteNotificationAsync(notificationId)).ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteNotification(notificationId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(true, okResult.Value);
    }

    [Fact]
    public async Task DeleteNotification_ReturnsNotFound_WhenNotificationIsNotFound()
    {
        // Arrange
        var notificationId = "1";
        _mockNotificationService.Setup(service => service.DeleteNotificationAsync(notificationId)).ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteNotification(notificationId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task UpdateNotification_ReturnsOkResult_WhenNotificationIsUpdated()
    {
        // Arrange
        var updateNotification = new NotificationDto.UpdateNotification
        {
            NotificationId = "1",
            Title_EN = "Updated Title",
            Message_EN = "Updated Message",
            IsRead = true
        };
        _mockNotificationService.Setup(service => service.UpdateNotificationAsync(updateNotification)).ReturnsAsync(true);

        // Act
        var result = await _controller.UpdateNotification(updateNotification);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public async Task UpdateNotification_ReturnsNotFound_WhenNotificationIsNotFound()
    {
        // Arrange
        var updateNotification = new NotificationDto.UpdateNotification
        {
            NotificationId = "1"
        };
        _mockNotificationService.Setup(service => service.UpdateNotificationAsync(updateNotification)).ReturnsAsync(false);

        // Act
        var result = await _controller.UpdateNotification(updateNotification);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task GetNotificationById_ReturnsOkResult_WithNotification()
    {
        // Arrange
        var notificationId = "1";
        var notification = new NotificationDto.ViewNotification
        {
            NotificationId = notificationId,
            Title = "Sample Notification",
            Message = "Sample Message",
            IsRead = false,
            Type = NotificationType.General
        };
        _mockNotificationService.Setup(service => service.GetNotificationById(notificationId, It.IsAny<string>())).ReturnsAsync(notification);

        // Act
        var result = await _controller.GetNotificationById(notificationId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(notification, okResult.Value);
    }

    [Fact]
    public async Task GetNotificationById_ReturnsNotFound_WhenNotificationIsNotFound()
    {
        // Arrange
        var notificationId = "1";
        _mockNotificationService.Setup(service => service.GetNotificationById(notificationId, It.IsAny<string>())).ReturnsAsync((NotificationDto.ViewNotification?)null);

        // Act
        var result = await _controller.GetNotificationById(notificationId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task GetUnreadNotifications_ReturnsOkResult_WithUnreadNotifications()
    {
        // Arrange
        var userId = "123";
        var notifications = CreateSampleNotifications();
        _mockNotificationService.Setup(service => service.GetUnreadUserNotifications(userId, It.IsAny<string>())).ReturnsAsync(notifications);

        // Act
        var result = await _controller.GetUnreadNotifications(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(notifications, okResult.Value);
    }

    [Fact]
    public async Task GetUnreadNotifications_ReturnsBadRequest_OnArgumentNullException()
    {
        // Arrange
        var userId = "123";
        _mockNotificationService.Setup(service => service.GetUnreadUserNotifications(userId, It.IsAny<string>())).ThrowsAsync(new ArgumentNullException("userId"));

        // Act
        var result = await _controller.GetUnreadNotifications(userId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task GetReadNotifications_ReturnsOkResult_WithReadNotifications()
    {
        // Arrange
        var userId = "123";
        var notifications = CreateSampleNotifications();
        _mockNotificationService.Setup(service => service.GetReadUserNotifications(userId, It.IsAny<string>())).ReturnsAsync(notifications);

        // Act
        var result = await _controller.GetReadNotifications(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(notifications, okResult.Value);
    }

    [Fact]
    public async Task GetReadNotifications_ReturnsBadRequest_OnArgumentNullException()
    {
        // Arrange
        var userId = "123";
        _mockNotificationService.Setup(service => service.GetReadUserNotifications(userId, It.IsAny<string>())).ThrowsAsync(new ArgumentNullException("userId"));

        // Act
        var result = await _controller.GetReadNotifications(userId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task GetUserNotificationsByType_ReturnsOkResult_WithNotificationsByType()
    {
        // Arrange
        var userId = "123";
        var type = NotificationType.General;
        var notifications = CreateSampleNotifications();
        _mockNotificationService.Setup(service => service.GetUserNotificationsByType(userId, type, It.IsAny<string>())).ReturnsAsync(notifications);

        // Act
        var result = await _controller.GetUserNotificationsByType(userId, type);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(notifications, okResult.Value);
    }

    [Fact]
    public async Task GetUserNotificationsByType_ReturnsBadRequest_OnArgumentNullException()
    {
        // Arrange
        var userId = "123";
        var type = NotificationType.General;
        _mockNotificationService.Setup(service => service.GetUserNotificationsByType(userId, type, It.IsAny<string>())).ThrowsAsync(new ArgumentNullException("userId"));

        // Act
        var result = await _controller.GetUserNotificationsByType(userId, type);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task GetUnreadNotificationCount_ReturnsOkResult_WithCorrectCount()
    {
        // Arrange
        var userId = "123";
        var notificationCount = new NotificationDto.NotificationCount { Count = 5 };
        _mockNotificationService.Setup(service => service.GetUnreadUserNotificationsCount(userId)).ReturnsAsync(notificationCount);

        // Act
        var result = await _controller.GetUnreadNotificationCount(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(notificationCount, okResult.Value);
    }

    [Fact]
    public async Task GetUnreadNotificationCount_ReturnsBadRequest_OnArgumentNullException()
    {
        // Arrange
        var userId = "123";
        _mockNotificationService
            .Setup(service => service.GetUnreadUserNotificationsCount(userId))
            .ThrowsAsync(new ArgumentNullException("userId"));

        // Act
        var result = await _controller.GetUnreadNotificationCount(userId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);

        // Access the anonymous object using reflection
        var badRequestValue = badRequestResult.Value;
        Assert.NotNull(badRequestValue);

        var messageProperty = badRequestValue.GetType().GetProperty("message");
        Assert.NotNull(messageProperty);

        var messageValue = messageProperty.GetValue(badRequestValue) as string;
        Assert.Equal("Invalid input: Value cannot be null. (Parameter 'userId')", messageValue);
    }

    [Fact]
    public async Task GetUnreadNotificationCount_ReturnsInternalServerError_OnException()
    {
        // Arrange
        var userId = "123";
        _mockNotificationService
            .Setup(service => service.GetUnreadUserNotificationsCount(userId))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        var result = await _controller.GetUnreadNotificationCount(userId);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);

        // Access the anonymous object using reflection
        var responseValue = statusResult.Value;
        Assert.NotNull(responseValue);

        var messageProperty = responseValue.GetType().GetProperty("message");
        var detailProperty = responseValue.GetType().GetProperty("detail");

        Assert.NotNull(messageProperty);
        Assert.NotNull(detailProperty);

        var messageValue = messageProperty.GetValue(responseValue) as string;
        var detailValue = detailProperty.GetValue(responseValue) as string;

        Assert.Equal("An unexpected error occurred.", messageValue);
        Assert.Equal("Test exception", detailValue);
    }


    [Fact]
    public async Task GetUnreadNotificationCount_ReturnsZeroCount_WhenUserHasNoUnreadNotifications()
    {
        // Arrange
        var userId = "123";
        var notificationCount = new NotificationDto.NotificationCount { Count = 0 };
        _mockNotificationService.Setup(service => service.GetUnreadUserNotificationsCount(userId)).ReturnsAsync(notificationCount);

        // Act
        var result = await _controller.GetUnreadNotificationCount(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(notificationCount, okResult.Value);
    }
}
