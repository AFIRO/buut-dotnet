using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Rise.Persistence;
using Rise.Shared.Enums;
using Rise.Shared.Notifications;
using Rise.Domain.Users;
using System.Net.Http.Headers;
using System.Text.Json;
using Rise.Domain.Notifications;
using Moq;
using System.Net;

namespace Rise.Server.Tests.E2E;

[Collection("IntegrationTests")]
public class NotificationControllerE2ETests : BaseControllerE2ETests
{
    public NotificationControllerE2ETests(CustomWebApplicationFactory<Program> factory) : base(factory) { }

    protected override void SeedData()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var address1 = new Address("Afrikalaan", "5");
        var address2 = new Address("Bataviabrug", "35");
        var adminUser = new User("auth0|admin", "Admin", "User", "admin@example.com", DateTime.UtcNow.AddYears(-30), address1, "+1234567890");
        adminUser.Roles.Add(new Role(RolesEnum.Admin));

        var normalUser = new User("auth0|user", "Normal", "User", "user@example.com", DateTime.UtcNow.AddYears(-25), address2, "+0987654321");
        normalUser.Roles.Add(new Role(RolesEnum.User));

        dbContext.Users.Add(adminUser);
        dbContext.Users.Add(normalUser);

        dbContext.Notifications.Add(new Notification(
            userId: normalUser.Id,
            title_EN: "Sample Notification",
            title_NL: "Voorbeeldmelding",
            message_EN: "This is a sample notification",
            message_NL: "Dit is een voorbeeldmelding",
            type: NotificationType.General
        ));

        dbContext.SaveChanges();
    }

    [Fact]
    public async Task GetAllNotifications_AsAdmin_Should_Return_Notifications()
    {
        // Arrange
        var token = GenerateJwtToken("admin", "Admin", "auth0|admin");
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/Notification");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        var notifications = await response.Content.ReadFromJsonAsync<IEnumerable<NotificationDto.ViewNotification>>(JsonOptions);

        Assert.NotNull(notifications);
        Assert.NotEmpty(notifications);
    }

    [Fact]
    public async Task GetAllNotifications_AsNonAdmin_Should_Return_Forbidden()
    {
        // Arrange
        var token = GenerateJwtToken("user", "User", "auth0|user");
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/Notification");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetUserNotifications_Should_Return_Notifications()
    {
        // Arrange
        var token = GenerateJwtToken("user", "User", "auth0|user");
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/Notification/user/auth0|user");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        var notifications = await response.Content.ReadFromJsonAsync<IEnumerable<NotificationDto.ViewNotification>>(JsonOptions);

        Assert.NotNull(notifications);
        Assert.NotEmpty(notifications);
    }


    [Fact]
    public async Task CreateNotification_Should_Create_Notification()
    {
        // Arrange
        var token = GenerateJwtToken("admin", "Admin", "auth0|admin");
        var newNotification = new NotificationDto.NewNotification
        {
            UserId = "auth0|user",
            Title_EN = "New Notification",
            Title_NL = "Nieuwe Melding",
            Message_EN = "This is a new notification",
            Message_NL = "Dit is een nieuwe melding",
            Type = NotificationType.General
        };
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/Notification")
        {
            Content = JsonContent.Create(newNotification)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        var createdNotification = await response.Content.ReadFromJsonAsync<NotificationDto.ViewNotification>(JsonOptions);

        Assert.NotNull(createdNotification);
        Assert.Equal(newNotification.Title_EN, createdNotification.Title);
    }

    [Fact]
    public async Task GetNotificationById_Should_Return_Notification()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notification = dbContext.Notifications.First();

        var token = GenerateJwtToken("user", "User", "auth0|user");
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/Notification/{notification.Id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        var returnedNotification = await response.Content.ReadFromJsonAsync<NotificationDto.ViewNotification>(JsonOptions);

        Assert.NotNull(returnedNotification);
        Assert.Equal(notification.Id, returnedNotification.NotificationId);
    }

    [Fact]
    public async Task DeleteNotification_Should_Remove_Notification()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notification = dbContext.Notifications.First();

        var token = GenerateJwtToken("admin", "Admin", "auth0|admin");
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/Notification/{notification.Id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();

        // Verify the notification is deleted
        var deletedNotification = dbContext.Notifications.AsNoTracking().FirstOrDefault(n => n.Id == notification.Id);
        Assert.NotNull(deletedNotification);
        Assert.True(deletedNotification.IsDeleted);
    }

    [Fact]
    public async Task UpdateNotification_Should_Update_Notification()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notification = dbContext.Notifications.First();

        var updatedNotification = new NotificationDto.UpdateNotification
        {
            NotificationId = notification.Id,
            Title_EN = "Updated Title",
            Message_EN = "Updated Message",
            IsRead = true
        };

        var token = GenerateJwtToken("admin", "Admin", "auth0|admin");
        var request = new HttpRequestMessage(HttpMethod.Put, "/api/Notification")
        {
            Content = JsonContent.Create(updatedNotification)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();

        // Verify the notification is updated
        var updatedNotificationInDb = dbContext.Notifications.AsNoTracking().FirstOrDefault(n => n.Id == notification.Id);
        Assert.NotNull(updatedNotificationInDb);
        Assert.Equal(updatedNotification.Title_EN, updatedNotificationInDb.Title_EN);
    }


    [Fact]
    public async Task GetNotificationById_WhenNotificationDoesNotExist_Should_Return_NotFound()
    {
        // Arrange
        var nonExistentNotificationId = Guid.NewGuid().ToString();
        var token = GenerateJwtToken("admin", "Admin", "auth0|admin");
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/Notification/{nonExistentNotificationId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateNotification_WhenNotificationDoesNotExist_Should_Return_NotFound()
    {
        // Arrange
        var token = GenerateJwtToken("admin", "Admin", "auth0|admin");
        var nonExistentNotificationId = Guid.NewGuid().ToString();

        var updatedNotification = new NotificationDto.UpdateNotification
        {
            NotificationId = nonExistentNotificationId,
            Title_EN = "Updated Title",
            Message_EN = "Updated Message",
            IsRead = true
        };

        var request = new HttpRequestMessage(HttpMethod.Put, "/api/Notification")
        {
            Content = JsonContent.Create(updatedNotification)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteNotification_WhenNotificationDoesNotExist_Should_Return_NotFound()
    {
        // Arrange
        var nonExistentNotificationId = Guid.NewGuid().ToString();
        var token = GenerateJwtToken("admin", "Admin", "auth0|admin");
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/Notification/{nonExistentNotificationId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }


}
