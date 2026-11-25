using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;
using DigitalWorkstation.Core.Abstractions.Network;
using MailKit.Net.Proxy;
using DigitalWorkstation.Core.Abstractions.Services;
using DigitalWorkstation.Core.Abstractions.Settings;
using DigitalWorkstation.Core.Common.Events;
using DigitalWorkstation.MailReader.Resources;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Pop3;
using MailKit.Security;

namespace DigitalWorkstation.MailReader;

/// <summary>Owns one account and one in-flight refresh. Network/MIME work runs off the UI thread;
/// only a complete snapshot for the current generation can replace the visible inbox.</summary>
public sealed partial class MailService : ObservableObject, IModuleService
{
    private readonly ISettingsService _settings;
    private readonly INetworkService? _network;
    private readonly ICredentialStore _credentials;
    private readonly IEventAggregator _events;
    private readonly Func<ImapClient> _createClient;
    private readonly Func<Pop3Client> _createPop3Client;
    private readonly IMailCache? _cache;
    private readonly DispatcherTimer? _pollTimer;
    private string? _cacheAccountKey;
    private bool _pendingAutomaticRefresh;
    private int _lifecycle;
    internal Task AutomaticRefresh { get; private set; } = Task.CompletedTask;
    private CancellationTokenSource? _request;
    private CancellationTokenSource? _loginRequest;
    private bool _applyingLogin;
    private int _generation;
    private int _refreshing;
    private volatile bool _running;
    private bool _resetting;
    private MailAccount _account;

    public MailService(ISettingsService settings, ICredentialStore credentials, IEventAggregator events, INetworkService network)
        : this(settings, credentials, events, () => new ImapClient { Timeout = 30000 },
            cache: new MailCache(MailCache.DefaultDirectory), automatic: true, network: network) { }

    internal MailService(ISettingsService settings, ICredentialStore credentials, IEventAggregator events)
        : this(settings, credentials, events, () => new ImapClient { Timeout = 30000 },
            cache: new MailCache(MailCache.DefaultDirectory), automatic: true) { }

    internal MailService(ISettingsService settings, ICredentialStore credentials, IEventAggregator events,
        Func<ImapClient> createClient, Func<Pop3Client>? createPop3Client = null,
        IMailCache? cache = null, bool automatic = false, TimeSpan? pollInterval = null, INetworkService? network = null)
    {
        _settings = settings;
        _credentials = credentials;
        _events = events;
        _network = network;
        _createClient = createClient;
        _createPop3Client = createPop3Client ?? (() => new Pop3Client { Timeout = 30000 });
        _cache = cache;
        if (automatic)
        {
            _pollTimer = new DispatcherTimer { Interval = pollInterval ?? TimeSpan.FromMinutes(5) };
            _pollTimer.Tick += (_, _) => RequestAutomaticRefresh();
        }
        _account = MailSettings.Read(settings);
    }

    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasMessages), nameof(ShowInboxWarning))]
    private IReadOnlyList<MailMessage> _messages = [];
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasSelectedMessage))] private MailMessage? _selectedMessage;
    [ObservableProperty] private string _status = MailResources.ConfigureHint;
    [ObservableProperty] private string _cacheStatus = "";
    [ObservableProperty] private string _password = "";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(ShowInboxWarning))] private string _inboxFeedback = MailResources.ConfigureHint;
    public bool HasMessages => Messages.Count > 0;
    public bool ShowInboxWarning => HasMessages && InboxFeedback.Length > 0;
    public bool HasSelectedMessage => SelectedMessage is not null;
    public string AccountDisplay => $"{_account.Protocol} · {_account.Username} · {_account.Server}";

    public void Start()
    {
        if (_running) return;
        _running = true;
        var lifecycle = Interlocked.Increment(ref _lifecycle);
        _events.GetEvent<SettingChangedEvent>().Subscribe(SettingsChanged);
        void SyncAccount()
        {
            if (!_running || lifecycle != _lifecycle) return;
            // Stop unsubscribes, so settings may have changed while this instance was idle.
            var account = MailSettings.Read(_settings);
            if (account != _account) ResetAccount(account);
        }
        if (Dispatcher.UIThread.CheckAccess()) SyncAccount(); else Dispatcher.UIThread.Post(SyncAccount);
        Dispatcher.UIThread.Post(() =>
        {
            if (!_running || lifecycle != _lifecycle) return;
            _pollTimer?.Start();
            _pendingAutomaticRefresh = _refreshing != 0;
            RequestAutomaticRefresh();
        });
    }
    private void RequestAutomaticRefresh()
    {
        if (!_running || _pollTimer is null) return;
        if (_refreshing != 0) return;
        if (string.IsNullOrWhiteSpace(_account.Server) && string.IsNullOrWhiteSpace(_account.Username))
        {
            InboxFeedback = Status = MailResources.ConfigureHint;
            return;
        }
        AutomaticRefresh = RefreshAsync();
    }
    public void Stop()
    {
        _running = false;
        _loginRequest?.Cancel();
        var lifecycle = Interlocked.Increment(ref _lifecycle);
        _events.GetEvent<SettingChangedEvent>().Unsubscribe(SettingsChanged);
        Interlocked.Increment(ref _generation);
        _request?.Cancel();
        void End()
        {
            if (lifecycle != _lifecycle) return;
            _pollTimer?.Stop();
            _pendingAutomaticRefresh = false;
            _cacheAccountKey = null;
            Password = ""; Messages = []; SelectedMessage = null; CacheStatus = "";
            InboxFeedback = "";
            _events.GetEvent<CloseMainDocumentsEvent>().Publish(MailMainView.ViewId);
        }
        if (Dispatcher.UIThread.CheckAccess()) End(); else Dispatcher.UIThread.Post(End);
    }
    private void SettingsChanged(SettingChanged changed)
    {
        if (!changed.SettingId.StartsWith("mail-reader.", StringComparison.Ordinal)) return;
        Interlocked.Increment(ref _generation);
        _request?.Cancel();
        void Reset()
        {
            ResetAccount(MailSettings.Read(_settings));
            if (_pollTimer is not null && _running && !_applyingLogin)
            {
                _pendingAutomaticRefresh = _refreshing != 0;
                RequestAutomaticRefresh();
            }
        }
        if (Dispatcher.UIThread.CheckAccess()) Reset(); else Dispatcher.UIThread.Post(Reset);
    }
    private void ResetAccount(MailAccount account)
    {
        if (account.Key != _account.Key)
            _events.GetEvent<CloseMainDocumentsEvent>().Publish(MailMainView.ViewId);
        _account = account;
        _resetting = true;
        Password = "";
        _resetting = false;
        Messages = [];
        SelectedMessage = null;
        _cacheAccountKey = null;
        CacheStatus = "";
        InboxFeedback = Status = MailResources.ConfigureHint;
        OnPropertyChanged(nameof(AccountDisplay));
    }
    partial void OnPasswordChanged(string value)
    {
        if (_resetting) return;
        if (IsBusy) Status = MailResources.Cancelled;
        Interlocked.Increment(ref _generation);
        _request?.Cancel();
    }
    [RelayCommand]
    private void OpenMessage(MailMessage message)
    {
        SelectedMessage = message;
        _events.GetEvent<OpenMainDocumentEvent>().Publish(new MainDocument(
            MailMainView.ViewId, message.Id, message.Subject, message));
    }
    internal async Task<string> AuthenticateAsync(ISettingGroupDraft draft, MailAccount account, string loginCode,
        bool rememberCode, CancellationToken token)
    {
        if (account != MailSettings.Read(draft)) return MailResources.LoginCancelled;
        if (!account.IsValid) return MailResources.InvalidConfiguration;
        if (string.IsNullOrEmpty(loginCode)) return MailResources.PasswordRequired;
        if (!_running || _loginRequest is not null) return MailResources.LoginUnavailable;
        using var request = CancellationTokenSource.CreateLinkedTokenSource(token);
        _loginRequest = request;
        var generation = _generation;
        try
        {
            await Task.Run(async () =>
            {
                using IMailService client = account.Protocol == MailProtocol.POP3 ? _createPop3Client() : _createClient();
                await LoginAsync(client, account, loginCode, request.Token);
                await client.DisconnectAsync(true, request.Token);
            }, request.Token);
            if (request.IsCancellationRequested || generation != _generation || account != MailSettings.Read(draft))
                return MailResources.LoginCancelled;

            // Saving the group publishes settings changes synchronously. Start its inbox refresh
            // once the verified session credential is available for the new account.
            _applyingLogin = true;
            try
            {
                if (!draft.TrySave()) return MailResources.LoginSettingsSaveFailed;
                if (!_running || _account != account || request.IsCancellationRequested) return MailResources.LoginCancelled;
                Password = loginCode;
            }
            finally { _applyingLogin = false; }

            if (_pollTimer is not null)
            {
                _pendingAutomaticRefresh = _refreshing != 0;
                RequestAutomaticRefresh();
            }
            if (!rememberCode) return MailResources.LoginSucceeded;
            // The account has committed. Leaving the editor can suppress its feedback,
            // but must allow this account's credential write to finish.
            bool saved;
            try { saved = await _credentials.SaveAsync(account.Key, loginCode, CancellationToken.None); }
            catch { saved = false; }
            if (!_running || account != _account) return MailResources.LoginCancelled;
            return saved ? MailResources.LoginSucceeded : $"{MailResources.LoginSucceeded} {MailResources.SessionOnly}";
        }
        catch (OperationCanceledException) { return MailResources.LoginCancelled; }
        catch (MailKit.Security.AuthenticationException) { return MailResources.AuthenticationFailed; }
        catch (SslHandshakeException) { return MailResources.TlsFailed; }
        catch (NetworkProxyException) { return MailResources.ProxyFailed; }
        catch { return MailResources.LoginConnectionFailed; }
        finally { _loginRequest = null; }
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
        var notice = "";
        var cacheWarning = "";
        IsBusy = true;
        InboxFeedback = "";
        try
        {
            if (!account.IsValid)
            {
                notice = Status = account.Server.Length == 0 && account.Username.Length == 0
                    ? MailResources.ConfigureHint : MailResources.InvalidConfiguration;
                return;
            }
            if (_cache is not null && _cacheAccountKey != account.Key)
            {
                try
                {
                    var cached = await Task.Run(() => _cache.ReadAsync(account.Key, request.Token), request.Token);
                    if (generation != _generation || !_running) return;
                    _cacheAccountKey = account.Key;
                    Messages = cached;
                    CacheStatus = cached.Count == 0 ? "" : string.Format(MailResources.CacheLoaded, cached.Count);
                }
                catch (OperationCanceledException) { throw; }
                catch { if (generation == _generation && _running) cacheWarning = CacheStatus = MailResources.CacheReadFailed; }
                if (generation != _generation || !_running) return;
            }
            Status = MailResources.Loading;
            if (string.IsNullOrEmpty(password))
            {
                var result = await _credentials.ReadAsync(account.Key, request.Token);
                if (generation != _generation || !_running) return;
                password = result.Password ?? "";
                if (password.Length == 0)
                {
                    Status = MailResources.PasswordRequired;
                    notice = result.SecureStorageAvailable ? MailResources.SignInHint : $"{MailResources.SignInHint} {MailResources.SessionOnly}";
                    return;
                }
            }
            var messages = await Task.Run(() => FetchAsync(account, password, request.Token), request.Token);
            if (generation != _generation || !_running) return;
            _cacheAccountKey = account.Key;
            var previousId = SelectedMessage?.Id;
            Messages = messages;
            SelectedMessage = messages.FirstOrDefault(message => message.Id == previousId);
            Status = messages.Count == 0 ? MailResources.Empty : string.Format(MailResources.Loaded, messages.Count);
            if (messages.Count == 0) notice = Status;
            if (_cache is not null)
            {
                try
                {
                    await Task.Run(() => _cache.WriteAsync(account.Key, messages, request.Token), request.Token);
                    if (generation == _generation && _running) CacheStatus = MailResources.CacheSaved;
                }
                catch (OperationCanceledException) { throw; }
                catch { if (generation == _generation && _running) cacheWarning = CacheStatus = MailResources.CacheWriteFailed; }
            }
        }
        catch (Exception failure)
        {
            if (_running && generation == _generation)
                notice = Status = failure switch
                {
                    OperationCanceledException => MailResources.Cancelled,
                    MailKit.Security.AuthenticationException => MailResources.AuthenticationFailed,
                    SslHandshakeException => MailResources.TlsFailed,
                    NetworkProxyException => MailResources.ProxyFailed,
                    Pop3IdentityException => MailResources.Pop3IdentityFailed,
                    _ => MailResources.ConnectionFailed
                };
        }
        finally
        {
            _request = null;
            if (_running && generation == _generation)
                InboxFeedback = string.Join(Environment.NewLine, new[] { notice, cacheWarning }.Where(text => text.Length > 0));
            IsBusy = false;
            Interlocked.Exchange(ref _refreshing, 0);
            if (_running && _pendingAutomaticRefresh)
            {
                _pendingAutomaticRefresh = false;
                Dispatcher.UIThread.Post(RequestAutomaticRefresh);
            }
        }
    }

    private async Task<IReadOnlyList<MailMessage>> FetchAsync(MailAccount account, string password, CancellationToken token)
    {
        if (account.Protocol == MailProtocol.POP3) return await FetchPop3Async(account, password, token);
        using var client = _createClient();
        // Default certificate validation remains enabled; StartTls is mandatory, never opportunistic.
        await LoginAsync(client, account, password, token);
        var inbox = client.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadOnly, token);
        var messages = new List<MailMessage>();
        if (inbox.Count > 0)
        {
            // Resolve the last server sequence positions to stable UIDs in this UIDVALIDITY epoch.
            var startIndex = Math.Max(0, inbox.Count - 100);
            var summaries = await inbox.FetchAsync(startIndex, -1, MessageSummaryItems.UniqueId, token);
            foreach (var summary in summaries.TakeLast(100))
            {
                token.ThrowIfCancellationRequested();
                using var mime = await inbox.GetMessageAsync(summary.UniqueId, token);
                messages.Add(MailContentReader.Read($"{account.Key}:{inbox.UidValidity}:{summary.UniqueId.Id}", mime));
            }
        }
        await client.DisconnectAsync(true, token);
        return messages.OrderByDescending(message => message.Date).ToArray();
    }

    private async Task LoginAsync(IMailService client, MailAccount account, string password, CancellationToken token)
    {
        await ConnectAsync(client, account, token);
        await client.AuthenticateAsync(account.Username, password, token);
    }

    private async Task ConnectAsync(IMailService client, MailAccount account, CancellationToken token)
    {
        var port = int.Parse(account.Port);
        var tls = account.Tls == MailTls.SslOnConnect ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
        var route = _network is null ? null : await _network.GetProxyAsync(MailPlugin.Id, token);
        if (route is null)
        {
            await client.ConnectAsync(account.Server, port, tls, token);
            return;
        }

        var host = route.Address.IdnHost;
        var proxyPort = route.Address.Port;
        IProxyClient proxy = (route.Address.Scheme, route.Credential) switch
        {
            ("http", null) => new HttpProxyClient(host, proxyPort),
            ("http", { } credential) => new HttpProxyClient(host, proxyPort, credential),
            ("socks5", null) => new Socks5Client(host, proxyPort),
            ("socks5", { } credential) => new Socks5Client(host, proxyPort, credential),
            _ => throw new NetworkProxyException()
        };
        Stream tunnel;
        try { tunnel = await proxy.ConnectAsync(account.Server, port, client.Timeout, token); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception) { throw new NetworkProxyException(); }

        // MailKit owns the stream after connecting and validates TLS against the mailbox host.
        try { await client.ConnectAsync(tunnel, account.Server, port, tls, token); }
        catch { tunnel.Dispose(); throw; }
    }

    private sealed class Pop3IdentityException : Exception { }

    private async Task<IReadOnlyList<MailMessage>> FetchPop3Async(MailAccount account, string password, CancellationToken token)
    {
        using var client = _createPop3Client();
        await LoginAsync(client, account, password, token);
        IList<string> uids;
        try { uids = await client.GetMessageUidsAsync(token); }
        catch (Exception error) when (error is NotSupportedException or Pop3CommandException or Pop3ProtocolException)
        { throw new Pop3IdentityException(); }
        if (uids.Count != client.Count || uids.Any(string.IsNullOrWhiteSpace)
            || uids.Distinct(StringComparer.Ordinal).Count() != uids.Count)
            throw new Pop3IdentityException();

        var messages = new List<MailMessage>();
        // Sequence positions are used only inside this session; UIDL is the persistent identity.
        for (var index = Math.Max(0, client.Count - 100); index < client.Count; index++)
        {
            using var mime = await client.GetMessageAsync(index, token);
            messages.Add(MailContentReader.Read($"{account.Key}:{uids[index]}", mime));
        }
        await client.DisconnectAsync(true, token);
        return messages.OrderByDescending(message => message.Date).ToArray();
    }
}
