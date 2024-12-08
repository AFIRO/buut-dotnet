using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Rise.Domain.Notifications;
using Rise.Persistence;
using Rise.Services.Notifications;
using Rise.Shared.Enums;
using Rise.Shared.Notifications;
using Shouldly;
using Xunit;

namespace Rise.Services.Tests.Notifications;

public class NotificationServiceTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ILogger<NotificationService>> _logger;
    private readonly Mock<IEmailService> _emailService;
    private readonly NotificationService _notificationService;

    public NotificationServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _logger = new Mock<ILogger<NotificationService>>();
        _notificationService = new NotificationService(_dbContext, _logger.Object, _emailService.Object);
    }

    [Fact]
    public async Task GetAllNotificationsAsync_ReturnsNotifications()
    {
        // Arrange
        _dbContext.Notifications.Add(new Notification("123", "Title EN", "Title NL", "Message EN", "Message NL", NotificationType.General));
        _dbContext.Notifications.Add(new Notification("456", "Another Title EN", "Another Title NL", "Another Message EN", "Another Message NL", NotificationType.Alert));
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _notificationService.GetAllNotificationsAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(2);
    }

    [Fact]
    public async Task GetAllUserNotifications_ReturnsUserNotifications()
    {
        // Arrange
        var userId = "123";
        _dbContext.Notifications.Add(new Notification(userId, "Title EN", "Title NL", "Message EN", "Message NL", NotificationType.General));
        _dbContext.Notifications.Add(new Notification(userId, "Another Title EN", "Another Title NL", "Another Message EN", "Another Message NL", NotificationType.Alert));
        _dbContext.Notifications.Add(new Notification("456", "Different User Title EN", "Different User Title NL", "Different User Message EN", "Different User Message NL", NotificationType.Alert));
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _notificationService.GetAllUserNotifications(userId);

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(2);
    }

    [Fact]
    public async Task CreateNotificationAsync_CreatesNotification()
    {
        // Arrange
        var newNotification = new NotificationDto.NewNotification
        {
            UserId = "123",
            Title_EN = "New Title EN",
            Title_NL = "New Title NL",
            Message_EN = "New Message EN",
            Message_NL = "New Message NL",
            Type = NotificationType.General
        };

        // Act
        var result = await _notificationService.CreateNotificationAsync(newNotification);

        // Assert
        result.ShouldNotBeNull();
        result.Title.ShouldBe(newNotification.Title_EN);
        result.Message.ShouldBe(newNotification.Message_EN);
        (await _dbContext.Notifications.CountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task DeleteNotificationAsync_DeletesNotification()
    {
        // Arrange
        var notification = new Notification("123", "Title EN", "Title NL", "Message EN", "Message NL", NotificationType.General);
        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _notificationService.DeleteNotificationAsync(notification.Id);

        // Assert
        result.ShouldBeTrue();
        var deletedNotification = await _dbContext.Notifications.FindAsync(notification.Id);
        deletedNotification.ShouldNotBeNull();
        deletedNotification.IsDeleted.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateNotificationAsync_UpdatesNotification()
    {
        // Arrange
        var notification = new Notification("123", "Old Title EN", "Old Title NL", "Old Message EN", "Old Message NL", NotificationType.General);
        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync();

        var updateDto = new NotificationDto.UpdateNotification
        {
            NotificationId = notification.Id,
            Title_EN = "Updated Title EN",
            Message_EN = "Updated Message EN"
        };

        // Act
        var result = await _notificationService.UpdateNotificationAsync(updateDto);

        // Assert
        result.ShouldBeTrue();
        var updatedNotification = await _dbContext.Notifications.FindAsync(notification.Id);
        updatedNotification.ShouldNotBeNull();
        updatedNotification.Title_EN.ShouldBe("Updated Title EN");
        updatedNotification.Message_EN.ShouldBe("Updated Message EN");
    }

    [Fact]
    public async Task GetNotificationById_ReturnsNotification()
    {
        // Arrange
        var notification = new Notification("123", "Title EN", "Title NL", "Message EN", "Message NL", NotificationType.General);
        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _notificationService.GetNotificationById(notification.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Title.ShouldBe(notification.Title_EN);
        result.Message.ShouldBe(notification.Message_EN);
    }

    [Fact]
    public async Task GetUnreadUserNotifications_ReturnsUnreadNotifications()
    {
        // Arrange
        var userId = "123";
        _dbContext.Notifications.Add(new Notification(userId, "Unread Title EN", "Unread Title NL", "Unread Message EN", "Unread Message NL", NotificationType.General) { IsRead = false });
        _dbContext.Notifications.Add(new Notification(userId, "Read Title EN", "Read Title NL", "Read Message EN", "Read Message NL", NotificationType.General) { IsRead = true });
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _notificationService.GetUnreadUserNotifications(userId);

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(1);
        result.First().IsRead.ShouldBeFalse();
    }

    [Fact]
    public async Task GetReadUserNotifications_ReturnsReadNotifications()
    {
        // Arrange
        var userId = "123";
        _dbContext.Notifications.Add(new Notification(userId, "Unread Title EN", "Unread Title NL", "Unread Message EN", "Unread Message NL", NotificationType.General) { IsRead = false });
        _dbContext.Notifications.Add(new Notification(userId, "Read Title EN", "Read Title NL", "Read Message EN", "Read Message NL", NotificationType.General) { IsRead = true });
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _notificationService.GetReadUserNotifications(userId);

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(1);
        result.First().IsRead.ShouldBeTrue();
    }

    [Fact]
    public async Task GetUserNotificationsByType_ReturnsNotificationsByType()
    {
        // Arrange
        var userId = "123";
        _dbContext.Notifications.Add(new Notification(userId, "General Title EN", "General Title NL", "General Message EN", "General Message NL", NotificationType.General));
        _dbContext.Notifications.Add(new Notification(userId, "Alert Title EN", "Alert Title NL", "Alert Message EN", "Alert Message NL", NotificationType.Alert));
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _notificationService.GetUserNotificationsByType(userId, NotificationType.General);

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(1);
        result.First().Type.ShouldBe(NotificationType.General);
    }

    [Fact]
    public async Task GetAllNotificationsAsync_ThrowsException_ReturnsNull()
    {
        // Arrange
        _dbContext.Dispose(); // Simulate an exception scenario by disposing of the context

        // Act & Assert
        await Should.ThrowAsync<Exception>(() => _notificationService.GetAllNotificationsAsync());
    }

    [Fact]
    public async Task GetAllUserNotifications_InvalidUserId_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() => _notificationService.GetAllUserNotifications(null));
    }

    [Fact]
    public async Task CreateNotificationAsync_NullNotification_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() => _notificationService.CreateNotificationAsync(null));
    }

    [Fact]
    public async Task DeleteNotificationAsync_InvalidId_ReturnsFalse()
    {
        // Act
        var result = await _notificationService.DeleteNotificationAsync("NonExistentId");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task UpdateNotificationAsync_InvalidId_ReturnsFalse()
    {
        // Arrange
        var updateDto = new NotificationDto.UpdateNotification
        {
            NotificationId = "NonExistentId",
            Title_EN = "Updated Title"
        };

        // Act
        var result = await _notificationService.UpdateNotificationAsync(updateDto);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task GetNotificationById_InvalidId_ReturnsNull()
    {
        // Act
        var result = await _notificationService.GetNotificationById("NonExistentId");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetUnreadUserNotifications_InvalidUserId_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() => _notificationService.GetUnreadUserNotifications(null));
    }

    [Fact]
    public async Task GetReadUserNotifications_InvalidUserId_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() => _notificationService.GetReadUserNotifications(null));
    }

    [Fact]
    public async Task GetUserNotificationsByType_InvalidUserId_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() => _notificationService.GetUserNotificationsByType(null, NotificationType.General));
    }


    [Fact]
    public async Task GetUnreadUserNotificationsCount_ReturnsCorrectCount()
    {
        // Arrange
        var userId = "123";
        _dbContext.Notifications.Add(new Notification(userId, "Unread Title EN", "Unread Title NL", "Unread Message EN", "Unread Message NL", NotificationType.General) { IsRead = false });
        _dbContext.Notifications.Add(new Notification(userId, "Another Unread Title EN", "Another Unread Title NL", "Another Unread Message EN", "Another Unread Message NL", NotificationType.Alert) { IsRead = false });
        _dbContext.Notifications.Add(new Notification(userId, "Read Title EN", "Read Title NL", "Read Message EN", "Read Message NL", NotificationType.General) { IsRead = true });
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _notificationService.GetUnreadUserNotificationsCount(userId);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetUnreadUserNotificationsCount_NoUnreadNotifications_ReturnsZero()
    {
        // Arrange
        var userId = "123";
        _dbContext.Notifications.Add(new Notification(userId, "Read Title EN", "Read Title NL", "Read Message EN", "Read Message NL", NotificationType.General) { IsRead = true });
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _notificationService.GetUnreadUserNotificationsCount(userId);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(0);
    }

    [Fact]
    public async Task GetUnreadUserNotificationsCount_UserHasNoNotifications_ReturnsZero()
    {
        // Arrange
        var userId = "123";

        // Act
        var result = await _notificationService.GetUnreadUserNotificationsCount(userId);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(0);
    }

    [Fact]
    public async Task GetUnreadUserNotificationsCount_InvalidUserId_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() => _notificationService.GetUnreadUserNotificationsCount(null));
    }

    [Fact]
    public async Task GetUnreadUserNotificationsCount_ExceptionDuringQuery_ThrowsException()
    {
        // Arrange
        var userId = "123";
        _dbContext.Dispose(); // Simulate an exception scenario by disposing of the context

        // Act & Assert
        await Should.ThrowAsync<Exception>(() => _notificationService.GetUnreadUserNotificationsCount(userId));
    }
}

