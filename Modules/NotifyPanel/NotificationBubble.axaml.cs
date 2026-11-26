using System.Diagnostics;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using DigitalWorkstation.Core.Abstractions.Notifications;
using DigitalWorkstation.Core.Common;
using DigitalWorkstation.Core.Services.Notifications;
using DigitalWorkstation.Core.UIPackage;

namespace DigitalWorkstation.NotifyPanel;

public partial class NotificationBubble : UserControl
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan FadeTime = TimeSpan.FromMilliseconds(150);
    private readonly NotifyService _service;
    private readonly DispatcherTimer _timer = new();
    private readonly Stopwatch _visibleTime = new();
    private NotifyMessage? _pending;
    private long _lastSequence;
    private bool _closing;
    private bool _attached;

    public NotificationBubble() : this(IoC.Provider.Resolve<NotifyService>()) { }

    public NotificationBubble(NotifyService service)
    {
        _service = service;
        InitializeComponent();
        Card.Transitions = [new DoubleTransition { Property = OpacityProperty, Duration = FadeTime }];
        _timer.Tick += (_, _) =>
        {
            if (_closing) ShowPending();
            else BeginClose();
        };
        Card.PointerEntered += (_, _) => PauseAutoClose();
        Card.PointerExited += (_, _) => ResumeAutoClose();
        DismissButton.Click += (_, _) => BeginClose();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attached = true;
        _service.NotificationPublished += OnPublished;
        if (_service.NotifyList.FirstOrDefault() is { } latest) Receive(latest);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _attached = false;
        _service.NotificationPublished -= OnPublished;
        _timer.Stop();
        _visibleTime.Reset();
        _pending = null;
        _closing = false;
        Card.IsVisible = false;
        Card.Opacity = 0;
        Card.DataContext = null;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnPublished(object? sender, NotifyMessage message)
    {
        if (Dispatcher.UIThread.CheckAccess()) Receive(message);
        else Dispatcher.UIThread.Post(() => Receive(message));
    }

    private void Receive(NotifyMessage message)
    {
        // 并发发布的回调可能乱序，启动期间的快照也可能与回调重合。
        if (!_attached || message.Sequence <= _lastSequence) return;
        _lastSequence = message.Sequence;
        _pending = message;
        if (Card.IsVisible) BeginClose();
        else ShowPending();
    }

    private void BeginClose()
    {
        if (_closing || !Card.IsVisible) return;
        _timer.Stop();
        _visibleTime.Stop();
        _closing = true;
        Card.Opacity = 0;
        _timer.Interval = FadeTime;
        _timer.Start();
    }

    private void ShowPending()
    {
        _timer.Stop();
        _visibleTime.Reset();
        _closing = false;
        var message = _pending;
        _pending = null;
        Card.DataContext = message;
        Card.IsVisible = message is not null;
        if (message is null) return;

        var (icon, color) = message.Level switch
        {
            NotifyLevel.Success => (Icons.Ready, Brushes.MediumSeaGreen),
            NotifyLevel.Warning => (Icons.Warning, Brushes.Goldenrod),
            NotifyLevel.Error => (Icons.Error, Brushes.Salmon),
            _ => (Icons.About, Brushes.CornflowerBlue)
        };
        LevelIcon.Data = StreamGeometry.Parse(icon);
        LevelIcon.Foreground = color;
        Card.Opacity = 1;
        if (!Card.IsPointerOver) ResumeAutoClose();
    }

    private void PauseAutoClose()
    {
        if (_closing) return;
        _timer.Stop();
        _visibleTime.Stop();
    }

    private void ResumeAutoClose()
    {
        if (!_attached || _closing || !Card.IsVisible || _visibleTime.IsRunning) return;
        var remaining = Lifetime - FadeTime - _visibleTime.Elapsed;
        if (remaining <= TimeSpan.Zero)
        {
            BeginClose();
            return;
        }
        _visibleTime.Start();
        _timer.Interval = remaining;
        _timer.Start();
    }
}
