using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;
using DigitalWorkstation.Core.Abstractions.Services;
using DigitalWorkstation.Core.Abstractions.Settings;
using DigitalWorkstation.Core.Abstractions.Regions;
using DigitalWorkstation.Core.Common.Events;
using DigitalWorkstation.MailReader.Resources;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;

namespace DigitalWorkstation.MailReader;

/// <summary>Owns one account and one in-flight refresh. Network/MIME work runs off the UI thread;
/// only a complete snapshot for the current generation can replace the visible inbox.</summary>
public sealed partial class MailService : ObservableObject, IModuleService
{
    private readonly ISettingsService _settings;
    private readonly ICredentialStore _credentials;
    private readonly IEventAggregator _events;
    private CancellationTokenSource? _request;
    private int _generation;
    private int _refreshing;
    private volatile bool _running;
    private bool _resetting;
    private MailAccount _account;

    public MailService(ISettingsService settings, ICredentialStore credentials, IEventAggregator events)
    {
        _settings = settings;
        _credentials = credentials;
        _events = events;
        _account = MailSettings.Read(settings);
    }

    [ObservableProperty] private IReadOnlyList<MailMessage> _messages = [];
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasSelectedMessage))] private MailMessage? _selectedMessage;
    [ObservableProperty] private string _status = MailResources.ConfigureHint;
    [ObservableProperty] private string _credentialStatus = MailResources.CredentialHint;
    [ObservableProperty] private string _password = "";
    [ObservableProperty] private bool _isBusy;
    public bool HasSelectedMessage => SelectedMessage is not null;
    public string AccountDisplay => $"IMAP · {_account.Username} · {_account.Server}";

    public void Start()
    {
        _running = true;
        _events.GetEvent<SettingChangedEvent>().Subscribe(SettingsChanged);
    }
    public void Stop()
    {
        _running = false;
        _events.GetEvent<SettingChangedEvent>().Unsubscribe(SettingsChanged);
        Interlocked.Increment(ref _generation);
        _request?.Cancel();
        Dispatcher.UIThread.Post(() => { Password = ""; Messages = []; SelectedMessage = null; });
    }
    private void SettingsChanged(SettingChanged changed)
    {
        if (!changed.SettingId.StartsWith("mail-reader.", StringComparison.Ordinal)) return;
        Interlocked.Increment(ref _generation);
        _request?.Cancel();
        void Reset()
        {
            _account = MailSettings.Read(_settings);
            _resetting = true;
            Password = "";
            _resetting = false;
            Messages = [];
            SelectedMessage = null;
            CredentialStatus = MailResources.CredentialHint;
            Status = MailResources.ConfigureHint;
            OnPropertyChanged(nameof(AccountDisplay));
        }
        if (Dispatcher.UIThread.CheckAccess()) Reset(); else Dispatcher.UIThread.Post(Reset);
    }
    partial void OnPasswordChanged(string value)
    {
        if (_resetting) return;
        if (IsBusy) Status = MailResources.Cancelled;
        Interlocked.Increment(ref _generation);
        _request?.Cancel();
    }
    partial void OnSelectedMessageChanged(MailMessage? value)
    {
        if (value is not null) _events.GetEvent<OpenMainViewEvent>().Publish(MailMainView.ViewId);
    }
    [RelayCommand] private void OpenSettings() => _events.GetEvent<OpenMainViewEvent>().Publish(WellKnownViews.Settings);
    [RelayCommand] private void OpenReading() => _events.GetEvent<OpenMainViewEvent>().Publish(MailMainView.ViewId);

    [RelayCommand]
    private async Task SavePasswordAsync()
    {
        if (!_running || !_account.IsValid || string.IsNullOrEmpty(Password)) { CredentialStatus = MailResources.PasswordRequired; return; }
        var generation = _generation;
        var key = _account.Key;
        // Saving credentials is separate from settings and never makes a network connection.
        var saved = await _credentials.SaveAsync(key, Password, CancellationToken.None);
        if (_running && generation == _generation)
            CredentialStatus = saved ? MailResources.CredentialSaved : MailResources.SessionOnly;
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (!_running || Interlocked.Exchange(ref _refreshing, 1) != 0) return;
        using var request = new CancellationTokenSource();
        _request = request;
        var generation = _generation;
        var account = _account;
        var password = Password;
        IsBusy = true;
        try
        {
            if (!account.IsValid) { Status = MailResources.InvalidConfiguration; return; }
            Status = MailResources.Loading;
            if (string.IsNullOrEmpty(password))
            {
                var result = await _credentials.ReadAsync(account.Key, request.Token);
                if (generation != _generation || !_running) return;
                CredentialStatus = result.SecureStorageAvailable ? MailResources.CredentialHint : MailResources.SessionOnly;
                password = result.Password ?? "";
            }
            if (string.IsNullOrEmpty(password)) { Status = MailResources.PasswordRequired; return; }
            var messages = await Task.Run(() => FetchAsync(account, password, request.Token), request.Token);
            if (generation != _generation || !_running) return;
            var previousId = SelectedMessage?.Id;
            Messages = messages;
            SelectedMessage = messages.FirstOrDefault(message => message.Id == previousId);
            Status = messages.Count == 0 ? MailResources.Empty : string.Format(MailResources.Loaded, messages.Count);
        }
        catch (OperationCanceledException) { if (_running && generation == _generation) Status = MailResources.Cancelled; }
        catch (MailKit.Security.AuthenticationException) { if (_running && generation == _generation) Status = MailResources.AuthenticationFailed; }
        catch (SslHandshakeException) { if (_running && generation == _generation) Status = MailResources.TlsFailed; }
        catch { if (_running && generation == _generation) Status = MailResources.ConnectionFailed; }
        finally
        {
            _request = null;
            IsBusy = false;
            Interlocked.Exchange(ref _refreshing, 0);
        }
    }

    private static async Task<IReadOnlyList<MailMessage>> FetchAsync(MailAccount account, string password, CancellationToken token)
    {
        using var client = new ImapClient { Timeout = 30000 };
        // Default certificate validation remains enabled; StartTls is mandatory, never opportunistic.
        await client.ConnectAsync(account.Server, int.Parse(account.Port), account.Tls == MailTls.SslOnConnect ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls, token);
        await client.AuthenticateAsync(account.Username, password, token);
        var inbox = client.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadOnly, token);
        var messages = new List<MailMessage>();
        if (inbox.Count > 0)
        {
            // Resolve the last server sequence positions to stable UIDs in this UIDVALIDITY epoch.
            var summaries = await inbox.FetchAsync(Math.Max(0, inbox.Count - 100), -1, MessageSummaryItems.UniqueId, token);
            foreach (var summary in summaries.TakeLast(100))
            {
                token.ThrowIfCancellationRequested();
                using var mime = await inbox.GetMessageAsync(summary.UniqueId, token);
                messages.Add(new MailMessage($"{account.Key}:{inbox.UidValidity}:{summary.UniqueId.Id}",
                    string.IsNullOrWhiteSpace(mime.Subject) ? MailResources.NoSubject : mime.Subject,
                    mime.From.ToString(), mime.To.ToString(), mime.Date, MailText.Read(mime)));
            }
        }
        await client.DisconnectAsync(true, token);
        return messages.OrderByDescending(message => message.Date).ToArray();
    }
}
