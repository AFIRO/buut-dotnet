using Rise.Shared.Enums;
using System.ComponentModel.DataAnnotations;


/// <summary>
/// Data Transfer Object for Notifications.
/// </summary>
public class NotificationDto
{
    /// <summary>
    /// Represents a new notification.
    /// </summary>
    public class NewNotification
    {

        /// <summary>
        /// Gets or sets the English title of the notification.
        /// </summary>
        [Required(ErrorMessage = "Title_EN_Required")]
        public string Title_EN { get; set; } = default!;
        /// <summary>
        /// Gets or sets the Dutch title of the notification.
        /// </summary>
        [Required(ErrorMessage = "Title_NL_Required")]
        public string Title_NL { get; set; } = default!;

        /// <summary>
        /// Gets or sets the English message of the notification.
        /// </summary>
        [Required(ErrorMessage = "Message_EN_Required")]
        public string Message_EN { get; set; } = default!;
        /// <summary>
        /// Gets or sets the Dutch message of the notification.
        /// </summary>
        [Required(ErrorMessage = "Message_NL_Required")]
        public string Message_NL { get; set; } = default!;
        /// <summary>
        /// Gets or sets the user ID associated with the notification.
        /// </summary>
        public string UserId { get; set; } = default!;
        /// <summary>
        /// Gets or sets the type of the notification.
        /// </summary>
        public NotificationType Type { get; set; }
        /// <summary>
        /// Gets or sets the related entity ID associated with the notification.
        /// </summary>
        public string? RelatedEntityId { get; set; }
    }

    /// <summary>
    /// Represents a viewable notification.
    /// </summary>
    public class ViewNotification
    {
        /// <summary>
        /// Gets or sets the notification ID.
        /// </summary>
        public string NotificationId { get; set; } = default!;
        /// <summary>
        /// Gets or sets the title of the notification.
        /// </summary>
        public string Title { get; set; } = default!;
        /// <summary>
        /// Gets or sets the message of the notification.
        /// </summary>
        public string Message { get; set; } = default!;
        /// <summary>
        /// Gets or sets a value indicating whether the notification has been read.
        /// </summary>
        public bool IsRead { get; set; } = false;
        /// <summary>
        /// Gets or sets the type of the notification.
        /// </summary>
        public NotificationType Type { get; set; }
        /// <summary>
        /// Gets or sets the creation date and time of the notification.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        /// <summary>
        /// Gets or sets the related entity ID associated with the notification.
        /// </summary>
        public string? RelatedEntityId { get; set; }
    }

    /// <summary>
    /// Represents the count of notifications.
    /// </summary>
    public class NotificationCount
    {
        /// <summary>
        /// Gets or sets the count of notifications.
        /// </summary>
        public int Count { get; set; } = 0;
    }

    /// <summary>
    /// Represents an updatable notification.
    /// </summary>
    public class UpdateNotification
    {
        /// <summary>
        /// Gets or sets the notification ID.
        /// </summary>
        public string NotificationId { get; set; } = default!;
        /// <summary>
        /// Gets or sets the English title of the notification.
        /// </summary>
        public string? Title_EN { get; set; } = default!;
        /// <summary>
        /// Gets or sets the Dutch title of the notification.
        /// </summary>
        public string? Title_NL { get; set; } = default!;
        /// <summary>
        /// Gets or sets the English message of the notification.
        /// </summary>
        public string? Message_EN { get; set; } = default!;
        /// <summary>
        /// Gets or sets the Dutch message of the notification.
        /// </summary>
        public string? Message_NL { get; set; } = default!;

        /// <summary>
        /// Gets or sets a value indicating whether the notification has been read.
        /// </summary>
        public bool IsRead { get; set; } = false;
        /// <summary>
        /// Gets or sets the type of the notification.
        /// </summary>
        public NotificationType? Type { get; set; }
        /// <summary>
        /// Gets or sets the related entity ID associated with the notification.
        /// </summary>
        public string? RelatedEntityId { get; set; }
    }
}