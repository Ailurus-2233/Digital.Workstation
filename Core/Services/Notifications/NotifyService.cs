using System.Collections.Immutable;
using DigitalWorkstation.Core.Abstractions.Notifications;

namespace DigitalWorkstation.Core.Services.Notifications;

public sealed class NotifyService : INotifyService
{
    private readonly Lock _gate = new();
    private ImmutableList<NotifyMessage> _messages = [];

    public IReadOnlyList<NotifyMessage> NotifyList
    {
        get { lock (_gate) return _messages; }
    }

    public event EventHandler? NotifyListChanged;
    public event EventHandler<NotifyMessage>? NotificationPublished;

    public void Notify(NotifyLevel level, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        NotifyMessage notification;
        lock (_gate)
        {
            notification = new NotifyMessage(level, message);
            _messages = _messages.Insert(0, notification);
        }
        NotifyListChanged?.Invoke(this, EventArgs.Empty);
        NotificationPublished?.Invoke(this, notification);
    }

    public void Remove(NotifyMessage message)
    {
        lock (_gate)
        {
            var updated = _messages.Remove(message);
            if (updated == _messages) return;
            _messages = updated;
        }
        NotifyListChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        lock (_gate)
        {
            if (_messages.IsEmpty) return;
            _messages = _messages.Clear();
        }
        NotifyListChanged?.Invoke(this, EventArgs.Empty);
    }
}
