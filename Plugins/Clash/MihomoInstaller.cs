using System.IO.Compression;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;

namespace DigitalWorkstation.Clash;

internal sealed class MihomoInstaller
{
    private const string Latest = "https://api.github.com/repos/MetaCubeX/mihomo/releases/latest";
    private static readonly HttpClient SharedClient = new() { Timeout = TimeSpan.FromSeconds(90) };
    private readonly HttpClient github;
    private readonly TimeSpan bodyTimeout;
    private string? resolved;

    internal MihomoInstaller() : this(SharedClient, TimeSpan.FromSeconds(90)) { }

    internal MihomoInstaller(HttpClient github, TimeSpan bodyTimeout)
    {
        this.github = github;
        this.bodyTimeout = bodyTimeout;
    }

    internal static string Executable(string folder) => Path.Combine(folder, OperatingSystem.IsWindows() ? "mihomo.exe" : "mihomo");

    internal async Task<string> ResolveAsync(string folder, CancellationToken token, bool forceCheck = false, Action? beforeReplace = null)
    {
        if (!forceCheck && resolved != null && File.Exists(resolved)) return resolved;
        var path = Executable(folder);
        var marker = path + ".sha256";
        string? cachedDigest = File.Exists(marker) && File.Exists(path) ? File.ReadAllText(marker).Trim() : null;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, Latest);
            request.Headers.UserAgent.ParseAdd("DigitalWorkstation-Clash/1.0");
            using var response = await github.SendAsync(request, token);
            response.EnsureSuccessStatusCode();
            var release = await response.Content.ReadFromJsonAsync<JsonElement>(token);
            var tag = release.GetProperty("tag_name").GetString();
            var platform = OperatingSystem.IsWindows() ? "windows" : OperatingSystem.IsMacOS() ? "darwin" :
                OperatingSystem.IsLinux() ? "linux" : throw new PlatformNotSupportedException();
            var arch = RuntimeInformation.OSArchitecture switch
            {
                Architecture.X64 => "amd64",
                Architecture.X86 => "386",
                Architecture.Arm64 => "arm64",
                _ => throw new PlatformNotSupportedException(ClashResources.UnsupportedArch)
            };
            var extension = OperatingSystem.IsWindows() ? "zip" : "gz";
            var prefix = $"mihomo-{platform}-{arch}";
            var assets = release.GetProperty("assets").EnumerateArray().ToDictionary(a => a.GetProperty("name").GetString()!);
            if (!(arch == "amd64" && assets.TryGetValue($"{prefix}-compatible-{tag}.{extension}", out var asset)) &&
                !assets.TryGetValue($"{prefix}-{tag}.{extension}", out asset))
                throw new InvalidDataException(string.Format(ClashResources.NoAsset, tag, platform, arch));
            var digest = asset.GetProperty("digest").GetString();
            if (digest == null || !digest.StartsWith("sha256:", StringComparison.Ordinal) || digest.Length != 71)
                throw new InvalidDataException(ClashResources.MissingDigest);
            if (cachedDigest != digest)
                await InstallAsync(path, asset.GetProperty("browser_download_url").GetString()!, digest, token, beforeReplace);
            return resolved = path;
        }
        catch (Exception ex) when (!forceCheck && cachedDigest != null && !token.IsCancellationRequested &&
            ex is HttpRequestException or IOException or OperationCanceledException)
        {
            System.Diagnostics.Trace.TraceWarning(string.Format(ClashResources.Fallback, ex.Message));
            return resolved = path;
        }
    }

    private async Task InstallAsync(string path, string url, string digest, CancellationToken token, Action? beforeReplace)
    {
        var staging = path + "." + Guid.NewGuid().ToString("N");
        var archive = staging + ".download";
        var metadata = staging + ".sha256";
        try
        {
            using (var file = new FileStream(archive, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
            {
                using var response = await github.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);
                response.EnsureSuccessStatusCode();
                using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
                deadline.CancelAfter(bodyTimeout);
                await response.Content.CopyToAsync(file, deadline.Token);
                file.Position = 0;
                var hash = await SHA256.HashDataAsync(file, token);
                if (!Convert.ToHexString(hash).Equals(digest[7..], StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException(ClashResources.InvalidDigest);
                file.Position = 0;
                using var output = new FileStream(staging, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                if (OperatingSystem.IsWindows())
                {
                    using var zip = new ZipArchive(file, ZipArchiveMode.Read, true);
                    var entry = zip.Entries.Single(e => e.Name.StartsWith("mihomo", StringComparison.OrdinalIgnoreCase)
                        && e.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
                    using var input = entry.Open();
                    await input.CopyToAsync(output, token);
                }
                else
                {
                    using var input = new GZipStream(file, CompressionMode.Decompress, true);
                    await input.CopyToAsync(output, token);
                    File.SetUnixFileMode(staging, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
                }
            }
            await File.WriteAllTextAsync(metadata, digest, token);
            token.ThrowIfCancellationRequested();
            beforeReplace?.Invoke();
            File.Move(staging, path, true);
            File.Move(metadata, path + ".sha256", true);
        }
        finally
        {
            File.Delete(staging);
            File.Delete(archive);
            File.Delete(metadata);
        }
    }
}
