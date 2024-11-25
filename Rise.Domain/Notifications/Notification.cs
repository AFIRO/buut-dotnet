namespace Rise.Domain.Notifications;

using System.ComponentModel.DataAnnotations.Schema;
using Rise.Shared.Enums;

/// <summary>
/// Represents a notification entity in the system
/// </summary>
public class Notification : Entity
{
    #region Fields
    private string _id = Guid.NewGuid().ToString();
    private string _title_EN = default!;
    private string _title_NL = default!;
    private string _message_EN = default!;
    private string _message_NL = default!;

    private string _userId = default!;
    private bool _isRead = false;
    [Column(TypeName = "nvarchar(50)")]
    private NotificationType _type;
    private string? _relatedEntityId;
    #endregion

    #region Constructors

    /// <summary>
    /// Private constructor for Entity Framework Core
    /// </summary>
    private Notification() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Notification"/> class with the specified details.
    /// </summary>
    /// <param name="userId">The ID of the user receiving the notification.</param>
    /// <param name="title_EN">The English title of the notification.</param>
    /// <param name="title_NL">The Dutch title of the notification.</param>
    /// <param name="message_EN">The English notification message.</param>
    /// <param name="message_NL">The Dutch notification message.</param>
    /// <param name="type">The type of the notification.</param>
    /// <param name="relatedEntityId">The ID of the related entity (optional).</param>
    public Notification(string userId, string title_EN, string title_NL, string message_EN, string message_NL, NotificationType type, bool isRead = false, string? relatedEntityId = null)
    {
        UserId = userId;
        Title_EN = title_EN;
        Message_EN = message_EN;
        Title_NL = title_NL;
        Message_NL = message_NL;
        Type = type;
        IsRead = isRead;
        CreatedAt = DateTime.UtcNow;
        RelatedEntityId = relatedEntityId;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the unique ID of the notification.
    /// </summary>
    public string Id
    {
        get => _id;
        set => _id = Guard.Against.NullOrWhiteSpace(value, nameof(Id));
    }

    /// <summary>
    /// Gets or sets the ID of the user receiving the notification.
    /// </summary>
    public string UserId
    {
        get => _userId;
        set => _userId = Guard.Against.NullOrWhiteSpace(value, nameof(UserId));
    }

    /// <summary>
    /// Gets or sets the English title of the notification.
    /// </summary>
    public string Title_EN
    {
        get => _title_EN;
        set => _title_EN = Guard.Against.NullOrWhiteSpace(value, nameof(Title_EN));
    }

    /// <summary>
    /// Gets or sets the notification message.
    /// </summary>
    public string Message_EN
    {
        get => _message_EN;
        set => _message_EN = Guard.Against.NullOrWhiteSpace(value, nameof(Message_EN));
    }

    /// <summary>
    /// Gets or sets the Dutch title of the notification.
    /// </summary>
    public string Title_NL
    {
        get => _title_NL;
        set => _title_NL = Guard.Against.NullOrWhiteSpace(value, nameof(Title_NL));
    }

    /// <summary>
    /// Gets or sets the notification message.
    /// </summary>
    public string Message_NL
    {
        get => _message_NL;
        set => _message_NL = Guard.Against.NullOrWhiteSpace(value, nameof(Message_NL));
    }

    /// <summary>
    /// Gets or sets a value indicating whether the notification has been read.
    /// </summary>
    public bool IsRead
    {
        get => _isRead;
        set => _isRead = value;
    }

    /// <summary>
    /// Gets or sets the type of the notification.
    /// </summary>
    public NotificationType Type
    {
        get => _type;
        set => _type = value;
    }


    /// <summary>
    /// Gets or sets the ID of the related entity (e.g., booking, boat, or battery).
    /// </summary>
    public string? RelatedEntityId
    {
        get => _relatedEntityId;
        set => _relatedEntityId = value;
    }

    #endregion
}
