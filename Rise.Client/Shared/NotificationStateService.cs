using System;

public class NotificationStateService
{
    public event Action? NotificationCountChanged;

    private int _notificationCount;
    public int NotificationCount
    {
        get => _notificationCount;
        private set
        {
            if (_notificationCount != value)
            {
                _notificationCount = value;
                NotifyCountChanged();
            }
        }
    }

    public void UpdateNotificationCount(int count)
    {
        NotificationCount = count;
    }

    private void NotifyCountChanged()
    {
        NotificationCountChanged?.Invoke();
    }
}
