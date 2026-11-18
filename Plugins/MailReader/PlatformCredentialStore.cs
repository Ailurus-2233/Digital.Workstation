using DigitalWorkstation.Core.Services.Credentials;

namespace DigitalWorkstation.MailReader;

public sealed record CredentialResult(bool SecureStorageAvailable, string? Password);

/// <summary>Secrets never pass through ordinary settings or diagnostic logging.</summary>
public interface ICredentialStore
{
    Task<CredentialResult> ReadAsync(string key, CancellationToken cancellationToken);
    Task<bool> SaveAsync(string key, string password, CancellationToken cancellationToken);
}

public sealed class PlatformCredentialStore : ICredentialStore
{
    private readonly PlatformPasswordStore _store = new("Digital.Workstation.MailReader", "Digital Workstation Mail");

    public async Task<CredentialResult> ReadAsync(string key, CancellationToken cancellationToken)
    {
        var result = await _store.ReadAsync(key, cancellationToken);
        return new(result.SecureStorageAvailable, result.Password);
    }

    public Task<bool> SaveAsync(string key, string password, CancellationToken cancellationToken) =>
        _store.SaveAsync(key, password, cancellationToken);
}
