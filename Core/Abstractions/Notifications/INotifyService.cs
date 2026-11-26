namespace DigitalWorkstation.Core.Abstractions.Notifications;

public enum NotifyLevel { Information, Success, Warning, Error }

public sealed class NotifyMessage(NotifyLevel level, string message)
{
    private static long _nextSequence;
    public long Sequence { get; } = Interlocked.Increment(ref _nextSequence);
    public NotifyLevel Level { get; } = level;
    public string Message { get; } = message;
    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.Now;
}

public interface INotifyService
{
    /// <summary>当前会话的只读快照，最新的通知在前。</summary>
    IReadOnlyList<NotifyMessage> NotifyList { get; }

    /// <summary>列表变化后在调用线程触发，订阅者可重新读取快照。</summary>
    event EventHandler? NotifyListChanged;

    void Notify(NotifyLevel level, string message);
}
