using System.Net;
using System.Security.Cryptography;
using System.Text;
using DigitalWorkstation.Core.Abstractions.Network;
using DigitalWorkstation.Core.Abstractions.Settings;
using DigitalWorkstation.Core.Services.Contributions;
using DigitalWorkstation.Core.Services.Credentials;
using DigitalWorkstation.Core.Services.Settings;

namespace DigitalWorkstation.Core.Services.Network;

public sealed record ProxySettings(bool Enabled, string Address, string Username, string[] Modules)
{
    public const string SettingId = "network.proxy";
    public static ProxySettings Default => new(false, "", "", []);

    // Missing entries read the original address/username key used by older versions.
    public Dictionary<string, int>? CredentialSlots { get; init; }

    internal Uri? ParseAddress() => ParseAddress(Address);

    internal static Uri? ParseAddress(string address)
    {
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "socks5") || string.IsNullOrEmpty(uri.Host) ||
            uri.UserInfo.Length != 0 || uri.AbsolutePath != "/" || uri.Query.Length != 0 ||
            uri.Fragment.Length != 0 || uri.Port == 0 || !uri.IsWellFormedOriginalString()) return null;
        return uri.Port < 0 ? new UriBuilder(uri) { Port = 1080 }.Uri : uri;
    }
}

public enum ProxySaveResult { Saved, InvalidAddress, UsernameRequired, PasswordRequired, StorageUnavailable, ConfigurationWriteFailed }

public enum ProxyTestOutcome { ResponseReceived, InvalidTarget, InvalidProxy, UsernameRequired, CredentialsUnavailable, TimedOut, ConnectionFailed }
public readonly record struct ProxyTestResult(ProxyTestOutcome Outcome, HttpStatusCode? Status = null);

public sealed class NetworkService(ISettingsService settings, ShellContributionCatalog catalog, PlatformPasswordStore passwords)
    : INetworkService, IDisposable
{
    private readonly Lock _httpGate = new();
    private readonly SemaphoreSlim _saveGate = new(1);
    private readonly SemaphoreSlim _credentialAccess = new(1);
    private readonly HttpTransport _direct = new(null);
    private HttpTransport? _proxied;
    private bool _disposed;
    public ProxySettings Configuration => settings.Get<ProxySettings>(ProxySettings.SettingId) ?? ProxySettings.Default;
    public IReadOnlyList<ProxyModule> Modules => catalog.Get<ProxyModule>().DistinctBy(module => module.Id).ToArray();

    public Task<ProxySaveResult> SaveAsync(ProxySettings configuration, string password) =>
        SaveAsync(configuration, password, updated =>
        {
            if (settings is SettingsService store)
                return store.TrySave(new Dictionary<string, object?> { [ProxySettings.SettingId] = updated });
            settings.Set(ProxySettings.SettingId, updated);
            return true;
        });

    public async Task<ProxySaveResult> SaveAsync(ProxySettings configuration, string password, Func<ProxySettings, bool> commit)
    {
        await _saveGate.WaitAsync();
        try { return await SaveCoreAsync(configuration, password, commit); }
        finally { _saveGate.Release(); }
    }

    private async Task<ProxySaveResult> SaveCoreAsync(ProxySettings configuration, string password, Func<ProxySettings, bool> commit)
    {
        var address = configuration.ParseAddress();
        if (address is null && (configuration.Enabled || configuration.Address.Length > 0))
            return ProxySaveResult.InvalidAddress;
        if (password.Length != 0 && configuration.Username.Length == 0) return ProxySaveResult.UsernameRequired;
        var slots = Configuration.CredentialSlots;
        if (configuration.Username.Length > 0 && (configuration.Enabled || password.Length > 0))
        {
            if (address is null) return ProxySaveResult.InvalidAddress;
            var identity = CredentialKey(address, configuration.Username);
            var slot = slots?.GetValueOrDefault(identity) ?? 0;
            if (password.Length > 0)
            {
                // Stage into the inactive slot. Only the committed JSON selects the new
                // password, even if the write fails or the process exits between stores.
                var next = slot == 1 ? 2 : 1;
                await _credentialAccess.WaitAsync();
                try
                {
                    if (!await passwords.SaveAsync(SlotKey(identity, next), password, CancellationToken.None))
                        return ProxySaveResult.StorageUnavailable;
                }
                finally { _credentialAccess.Release(); }
                slots = new Dictionary<string, int>(slots ?? []) { [identity] = next };
            }
            else
            {
                var stored = await passwords.ReadAsync(SlotKey(identity, slot), CancellationToken.None);
                if (!stored.SecureStorageAvailable) return ProxySaveResult.StorageUnavailable;
                if (string.IsNullOrEmpty(stored.Password)) return ProxySaveResult.PasswordRequired;
            }
        }
        return commit(configuration with { CredentialSlots = slots })
            ? ProxySaveResult.Saved : ProxySaveResult.ConfigurationWriteFailed;
    }

    public async Task<NetworkProxy?> GetProxyAsync(string moduleId, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        var config = Configuration;
        if (!config.Enabled || config.Modules?.Contains(moduleId, StringComparer.Ordinal) != true ||
            !Modules.Any(module => module.Id == moduleId)) return null;
        var address = config.ParseAddress() ?? throw new NetworkProxyException();
        return await CreateProxyAsync(address, config.Username, "", token).ConfigureAwait(false);
    }

    private async Task<NetworkProxy> CreateProxyAsync(Uri address, string username, string password, CancellationToken token)
    {
        NetworkCredential? credentials = null;
        if (!string.IsNullOrEmpty(username))
        {
            if (password.Length == 0)
            {
                var identity = CredentialKey(address, username);
                // A later rotation can reuse an old slot. Resolve the active key and
                // finish its read before allowing that slot to be overwritten.
                await _credentialAccess.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    var key = SlotKey(identity, Configuration.CredentialSlots?.GetValueOrDefault(identity) ?? 0);
                    var stored = await passwords.ReadAsync(key, token).ConfigureAwait(false);
                    password = stored.Password ?? throw new NetworkProxyException();
                    if (password.Length == 0) throw new NetworkProxyException();
                }
                finally { _credentialAccess.Release(); }
            }
            credentials = new NetworkCredential(username, password);
        }
        return new NetworkProxy(address, credentials);
    }

    // Probe the draft through a fresh connection, independent of saved module choices or pooled connections.
    public async Task<ProxyTestResult> TestProxyAsync(string target, string proxyAddress, string username, string password,
        CancellationToken token = default)
    {
        target = target.Trim();
        var url = target.Contains("://", StringComparison.Ordinal) ? target : "https://" + target;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var destination) || destination.Scheme is not ("http" or "https")
            || destination.Host.Length == 0 || destination.UserInfo.Length != 0 || !destination.IsWellFormedOriginalString())
            return new(ProxyTestOutcome.InvalidTarget);
        var address = ProxySettings.ParseAddress(proxyAddress.Trim());
        if (address is null) return new(ProxyTestOutcome.InvalidProxy);
        username = username.Trim();
        if (password.Length > 0 && username.Length == 0) return new(ProxyTestOutcome.UsernameRequired);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        try
        {
            var proxy = await CreateProxyAsync(address, username, password, timeout.Token).ConfigureAwait(false);
            using var client = new HttpClient(new SocketsHttpHandler { Proxy = proxy, AllowAutoRedirect = false, UseCookies = false });
            using var request = new HttpRequestMessage(HttpMethod.Get, destination);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
            return new(ProxyTestOutcome.ResponseReceived, response.StatusCode);
        }
        catch (NetworkProxyException) { return new(ProxyTestOutcome.CredentialsUnavailable); }
        catch (OperationCanceledException) when (!token.IsCancellationRequested) { return new(ProxyTestOutcome.TimedOut); }
        catch (HttpRequestException ex) { return new(ProxyTestOutcome.ConnectionFailed, ex.StatusCode); }
    }

    private static string CredentialKey(Uri address, string username) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"network.proxy\n{address.AbsoluteUri}\n{username}")));

    private static string SlotKey(string identity, int slot) => slot == 0 ? identity :
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{identity}:{slot}")));

    public async Task<HttpResponseMessage> SendAsync(string moduleId, HttpRequestMessage request, CancellationToken token = default)
    {
        var route = await GetProxyAsync(moduleId, token).ConfigureAwait(false);
        HttpTransport transport;
        lock (_httpGate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            transport = _direct;
            if (route is not null)
            {
                if (_proxied is null || !_proxied.Matches(route))
                {
                    _proxied?.Retire();
                    _proxied = new HttpTransport(route);
                }
                transport = _proxied;
            }
            transport.Users++;
        }
        try { return await transport.Client.SendAsync(request, token).ConfigureAwait(false); }
        finally
        {
            lock (_httpGate)
            {
                transport.Users--;
                if (transport.Retired && transport.Users == 0) transport.Client.Dispose();
            }
        }
    }

    public void Dispose()
    {
        lock (_httpGate)
        {
            if (_disposed) return;
            _disposed = true;
            _direct.Retire();
            _proxied?.Retire();
        }
    }

    // Reuse connection pools. Requests already using a retired proxy can finish on that route.
    private sealed class HttpTransport(NetworkProxy? proxy)
    {
        public HttpClient Client { get; } = new(new SocketsHttpHandler
        {
            UseProxy = proxy is not null, Proxy = proxy, UseCookies = false,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5)
        });
        public int Users { get; set; }
        public bool Retired { get; private set; }
        public bool Matches(NetworkProxy next) => proxy?.Address == next.Address &&
            proxy?.Credential?.UserName == next.Credential?.UserName && proxy?.Credential?.Password == next.Credential?.Password;
        public void Retire()
        {
            Retired = true;
            if (Users == 0) Client.Dispose();
        }
    }
}
