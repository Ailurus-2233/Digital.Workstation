using Avalonia.Controls;
using Avalonia.Interactivity;
using DigitalWorkstation.MailReader.Resources;

namespace DigitalWorkstation.MailReader;

public partial class MailReadingView : UserControl
{
    public MailReadingView() : this(DigitalWorkstation.Core.Common.IoC.Provider.Resolve<MailService>()) { }

    public MailReadingView(MailService service)
    {
        InitializeComponent();
        DataContext = service;
    }
    private async void CopyBody(object? sender, RoutedEventArgs args)
    {
        if (DataContext is not MailService { SelectedMessage: { } message } service) return;
        try
        {
            if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
                await clipboard.SetTextAsync(message.Body);
        }
        catch { service.Status = MailResources.CopyFailed; }
    }
}
