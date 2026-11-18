using System.Net;

namespace DigitalWorkstation.Core.Abstractions.Network;

/// <summary>Resolve once at the start of each operation so the next operation observes settings changes.</summary>
public interface INetworkService
{
    Task<NetworkProxy?> GetProxyAsync(string moduleId, CancellationToken token = default);

    /// <summary>Uses the current module route for this request. The caller owns the buffered response.</summary>
    Task<HttpResponseMessage> SendAsync(string moduleId, HttpRequestMessage request, CancellationToken token = default);
}

/// <summary>An explicit proxy also applies to local destinations.</summary>
public sealed class NetworkProxy(Uri address, NetworkCredential? credential) : IWebProxy
{
    public Uri Address { get; } = address;
    public NetworkCredential? Credential { get; } = credential;
    public Uri GetProxy(Uri destination) => Address;
    public bool IsBypassed(Uri host) => false;
    ICredentials? IWebProxy.Credentials
    {
        get => Credential;
        set => throw new NotSupportedException();
    }
}

/// <summary>Register a shell contribution with a stable module or plugin ID to enable selection in settings.</summary>
public sealed record ProxyModule(string Id, string DisplayName);

public sealed class NetworkProxyException() : IOException("The configured proxy is unavailable.");
