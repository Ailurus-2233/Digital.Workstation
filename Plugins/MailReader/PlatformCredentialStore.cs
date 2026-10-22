using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace DigitalWorkstation.MailReader;

public sealed record CredentialResult(bool SecureStorageAvailable, string? Password);

/// <summary>Secrets never pass through ordinary settings or diagnostic logging.</summary>
public interface ICredentialStore
{
    Task<CredentialResult> ReadAsync(string key, CancellationToken cancellationToken);
    Task<bool> SaveAsync(string key, string password, CancellationToken cancellationToken);
}

/// <summary>DPAPI on Windows, login Keychain on macOS, Secret Service via secret-tool on Linux.
/// Missing/locked/denied stores fall back explicitly to session credentials. No shell is used.</summary>
public sealed class PlatformCredentialStore : ICredentialStore
{
    public async Task<CredentialResult> ReadAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                var path = CredentialPath(key);
                if (!File.Exists(path)) return new(true, null);
                var encrypted = await File.ReadAllBytesAsync(path, cancellationToken);
                var bytes = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
                try { return new(true, Encoding.UTF8.GetString(bytes)); }
                finally { CryptographicOperations.ZeroMemory(bytes); }
            }
            if (OperatingSystem.IsMacOS()) return await Task.Run(() => MacRead(key), cancellationToken);
            if (OperatingSystem.IsLinux())
            {
                var result = await SecretToolAsync(["lookup", "application", "Digital.Workstation.MailReader", "account", key], null, cancellationToken);
                return new(result.Code == 0 || (result.Code == 1 && !result.HadError), result.Code == 0 ? result.Output.TrimEnd('\r', '\n') : null);
            }
        }
        catch (OperationCanceledException) { throw; }
        catch { /* Store errors are intentionally not logged: provider messages can contain secrets. */ }
        return new(false, null);
    }

    public async Task<bool> SaveAsync(string key, string password, CancellationToken cancellationToken)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                byte[] encrypted;
                try { encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser); }
                finally { CryptographicOperations.ZeroMemory(bytes); }
                var path = CredentialPath(key);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                var temp = path + ".tmp";
                await File.WriteAllBytesAsync(temp, encrypted, cancellationToken);
                File.Move(temp, path, true);
                return true;
            }
            if (OperatingSystem.IsMacOS()) return await Task.Run(() => MacSave(key, password), cancellationToken);
            if (OperatingSystem.IsLinux())
                return (await SecretToolAsync(["store", "--label=Digital Workstation Mail", "application", "Digital.Workstation.MailReader", "account", key], password, cancellationToken)).Code == 0;
        }
        catch (OperationCanceledException) { throw; }
        catch { }
        return false;
    }

    private static string CredentialPath(string key) => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Digital.Workstation", "credentials", key + ".bin");

    private static async Task<(int Code, string Output, bool HadError)> SecretToolAsync(string[] arguments, string? password, CancellationToken token)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        using var process = new Process { StartInfo = new ProcessStartInfo("secret-tool")
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true
        }};
        foreach (var arg in arguments) process.StartInfo.ArgumentList.Add(arg);
        process.Start();
        try
        {
            var output = process.StandardOutput.ReadToEndAsync(timeout.Token);
            var error = process.StandardError.ReadToEndAsync(timeout.Token);
            if (password is not null) await process.StandardInput.WriteAsync(password.AsMemory(), timeout.Token);
            process.StandardInput.Close();
            await process.WaitForExitAsync(timeout.Token);
            var hadError = (await error).Length != 0; // Never expose provider diagnostics.
            return (process.ExitCode, await output, hadError);
        }
        catch
        {
            if (!process.HasExited) process.Kill(true);
            if (token.IsCancellationRequested) throw;
            return (-1, "", true);
        }
    }

    private const string Security = "/System/Library/Frameworks/Security.framework/Security";
    private static readonly byte[] Service = Encoding.UTF8.GetBytes("Digital.Workstation.MailReader");
    [DllImport(Security)] private static extern int SecKeychainFindGenericPassword(IntPtr keychain, uint serviceLength, byte[] service,
        uint accountLength, byte[] account, out uint length, out IntPtr data, out IntPtr item);
    [DllImport(Security)] private static extern int SecKeychainAddGenericPassword(IntPtr keychain, uint serviceLength, byte[] service,
        uint accountLength, byte[] account, uint length, byte[] data, out IntPtr item);
    [DllImport(Security)] private static extern int SecKeychainItemModifyAttributesAndData(IntPtr item, IntPtr attributes, uint length, byte[] data);
    [DllImport(Security)] private static extern int SecKeychainItemFreeContent(IntPtr attributes, IntPtr data);
    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")] private static extern void CFRelease(IntPtr item);

    private static CredentialResult MacRead(string key)
    {
        var account = Encoding.UTF8.GetBytes(key);
        var status = SecKeychainFindGenericPassword(IntPtr.Zero, (uint)Service.Length, Service, (uint)account.Length, account, out var length, out var data, out var item);
        if (status != 0) return new(status == -25300, null); // errSecItemNotFound
        try { return new(true, Marshal.PtrToStringUTF8(data, checked((int)length))); }
        finally { SecKeychainItemFreeContent(IntPtr.Zero, data); if (item != IntPtr.Zero) CFRelease(item); }
    }
    private static bool MacSave(string key, string password)
    {
        var account = Encoding.UTF8.GetBytes(key);
        var secret = Encoding.UTF8.GetBytes(password);
        IntPtr item = IntPtr.Zero;
        try
        {
            var status = SecKeychainFindGenericPassword(IntPtr.Zero, (uint)Service.Length, Service, (uint)account.Length, account, out _, out var data, out item);
            if (status == 0)
            {
                SecKeychainItemFreeContent(IntPtr.Zero, data);
                return SecKeychainItemModifyAttributesAndData(item, IntPtr.Zero, (uint)secret.Length, secret) == 0;
            }
            if (status != -25300) return false;
            return SecKeychainAddGenericPassword(IntPtr.Zero, (uint)Service.Length, Service, (uint)account.Length, account, (uint)secret.Length, secret, out item) == 0;
        }
        finally { CryptographicOperations.ZeroMemory(secret); if (item != IntPtr.Zero) CFRelease(item); }
    }
}
