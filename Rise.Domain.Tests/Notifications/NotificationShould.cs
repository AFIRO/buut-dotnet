using System;
using Rise.Domain.Notifications;
using Rise.Shared.Enums;
using Shouldly;
using Xunit;

namespace Rise.Domain.Tests.Notifications
{
    public class NotificationShould
    {
        [Fact]
        public void ShouldCreateCorrectNotification()
        {
            // Arrange
            var userId = "123";
            var title_EN = "Test Title EN";
            var title_NL = "Test Title NL";
            var message_EN = "Test Message EN";
            var message_NL = "Test Message NL";
            var type = NotificationType.General;

            // Act
            var notification = new Notification(userId, title_EN, title_NL, message_EN, message_NL, type);

            // Assert
            notification.UserId.ShouldBe(userId);
            notification.Title_EN.ShouldBe(title_EN);
            notification.Title_NL.ShouldBe(title_NL);
            notification.Message_EN.ShouldBe(message_EN);
            notification.Message_NL.ShouldBe(message_NL);
            notification.Type.ShouldBe(type);
            notification.IsRead.ShouldBe(false);
            notification.CreatedAt.ShouldBeOfType<DateTime>();
        }

        [Fact]
        public void ShouldThrowArgumentNullException_WhenUserIdIsNullOrEmpty()
        {
            // Act
            Action action = () => new Notification(null, "Title EN", "Title NL", "Message EN", "Message NL", NotificationType.General);

            // Assert
            action.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("UserId");
        }

        [Fact]
        public void ShouldThrowArgumentNullException_WhenTitle_ENIsNullOrEmpty()
        {
            // Act
            Action action = () => new Notification("123", null, "Title NL", "Message EN", "Message NL", NotificationType.General);

            // Assert
            action.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("Title_EN");
        }

        [Fact]
        public void ShouldThrowArgumentNullException_WhenTitle_NLIsNullOrEmpty()
        {
            // Act
            Action action = () => new Notification("123", "Title EN", null, "Message EN", "Message NL", NotificationType.General);

            // Assert
            action.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("Title_NL");
        }

        [Fact]
        public void ShouldThrowArgumentNullException_WhenMessage_ENIsNullOrEmpty()
        {
            // Act
            Action action = () => new Notification("123", "Title EN", "Title NL", null, "Message NL", NotificationType.General);

            // Assert
            action.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("Message_EN");
        }

        [Fact]
        public void ShouldThrowArgumentNullException_WhenMessage_NLIsNullOrEmpty()
        {
            // Act
            Action action = () => new Notification("123", "Title EN", "Title NL", "Message EN", null, NotificationType.General);

            // Assert
            action.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("Message_NL");
        }

        [Fact]
        public void ShouldMarkNotificationAsRead()
        {
            // Arrange
            var notification = new Notification("123", "Title EN", "Title NL", "Message EN", "Message NL", NotificationType.General);

            // Act
            notification.IsRead = true;

            // Assert
            notification.IsRead.ShouldBeTrue();
        }

        [Fact]
        public void ShouldSetRelatedEntityId()
        {
            // Arrange
            var notification = new Notification("123", "Title EN", "Title NL", "Message EN", "Message NL", NotificationType.General);
            var relatedEntityId = "RelatedEntity123";

            // Act
            notification.RelatedEntityId = relatedEntityId;

            // Assert
            notification.RelatedEntityId.ShouldBe(relatedEntityId);
        }
    }
}
