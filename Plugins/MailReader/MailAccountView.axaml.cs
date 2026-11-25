using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DigitalWorkstation.Core.Abstractions.Settings;
using DigitalWorkstation.MailReader.Resources;

namespace DigitalWorkstation.MailReader;

public partial class MailAccountView : UserControl
{
    private readonly MailService _service;
    private CancellationTokenSource? _request;
    private MailAccount? _inputAccount;
    private INotifyPropertyChanged? _group;

    public MailAccountView() : this(DigitalWorkstation.Core.Common.IoC.Provider.Resolve<MailService>()) { }

    public MailAccountView(MailService service)
    {
        _service = service;
        InitializeComponent();
        LoginCode.TextChanged += (_, _) => ClearFeedback();
        DataContextChanged += (_, _) =>
        {
            if (_group is not null) _group.PropertyChanged -= OnDraftChanged;
            _group = DataContext as INotifyPropertyChanged;
            if (_group is not null) _group.PropertyChanged += OnDraftChanged;
            ResetLoginInput(DataContext is ISettingGroupDraft draft ? MailSettings.Read(draft) : null);
        };
        DetachedFromVisualTree += (_, _) =>
        {
            ResetLoginInput(_inputAccount);
        };
    }

    private void OnDraftChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (DataContext is not ISettingGroupDraft draft) return;
        var account = MailSettings.Read(draft);
        if (account != _inputAccount) ResetLoginInput(account);
    }

    private void ResetLoginInput(MailAccount? account)
    {
        ClearFeedback();
        LoginCode.Clear();
        RememberCode.IsChecked = false;
        _inputAccount = account;
    }

    private void ClearFeedback()
    {
        _request?.Cancel();
        Feedback.Text = "";
    }

    private async void Authenticate(object? sender, RoutedEventArgs e)
    {
        if (_request is not null || DataContext is not ISettingGroupDraft draft || _inputAccount is not { } account) return;
        using var request = new CancellationTokenSource();
        _request = request;
        AuthenticateButton.IsEnabled = RememberCode.IsEnabled = false;
        Feedback.Text = MailResources.Authenticating;
        try
        {
            var status = await _service.AuthenticateAsync(draft, account, LoginCode.Text ?? "", RememberCode.IsChecked == true, request.Token);
            if (!request.IsCancellationRequested) Feedback.Text = status;
        }
        finally
        {
            _request = null;
            AuthenticateButton.IsEnabled = RememberCode.IsEnabled = true;
        }
    }
}
