using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DigitalWorkstation.Core.Abstractions.Contributions;
using DigitalWorkstation.Core.Abstractions.Notifications;
using DigitalWorkstation.Core.Services.Notifications;

namespace DigitalWorkstation.NotifyPanel;

public sealed partial class NotifyPanelViewModel : ObservableObject, IToolViewBadge
{
    private readonly NotifyService _service;
    private int _refreshPending;

    public NotifyPanelViewModel(NotifyService service)
    {
        _service = service;
        _service.NotifyListChanged += OnListChanged;
        _messages = _service.NotifyList;
    }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(BadgeText))]
    private IReadOnlyList<NotifyMessage> _messages;

    public string? BadgeText => Messages.Count switch
    {
        0 => null,
        >= 10 => "*",
        var count => count.ToString()
    };

    private void OnListChanged(object? sender, EventArgs args)
    {
        if (Dispatcher.UIThread.CheckAccess())
            Messages = _service.NotifyList;
        else if (Interlocked.Exchange(ref _refreshPending, 1) == 0)
            Dispatcher.UIThread.Post(() =>
            {
                Interlocked.Exchange(ref _refreshPending, 0);
                Messages = _service.NotifyList;
            });
    }

    [RelayCommand]
    private void Remove(NotifyMessage message) => _service.Remove(message);

    [RelayCommand]
    private void Clear() => _service.Clear();
}
