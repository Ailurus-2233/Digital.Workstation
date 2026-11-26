using Avalonia.Threading;
using DigitalWorkstation.Core.Abstractions.Settings;
using DigitalWorkstation.Core.Common.Events;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using DigitalWorkstation.Core.Abstractions.Services;

namespace DigitalWorkstation.Clash;

internal enum ClashOperation { Idle, Other, UpdatingSubscriptions, Measuring, SelectingNode }

public sealed class ClashService : IModuleService
{
    private readonly string directory;
    private readonly string coreFolder;
    internal string CorePath => MihomoInstaller.Executable(coreFolder);
    private readonly Uri delayTarget;
    private readonly CancellationTokenSource lifetime = new();
    private readonly SemaphoreSlim operationGate = new(1, 1);
    private readonly HttpClient download = new(new HttpClientHandler { UseProxy = false }) { MaxResponseContentBufferSize = 16 * 1024 * 1024 };
    private readonly MihomoInstaller installer = new();
    private Engine? engine;
    private string SettingsPath => Path.Combine(directory, "settings.json");
    public Preferences Settings { get; private set; } = new();
    public List<Node> Nodes { get; private set; } = [];
    private string subscriptionError = "";
    private string operationMessage = "";
    public string Message => (subscriptionError, operationMessage) switch
    {
        ("", _) => operationMessage,
        (_, "") => subscriptionError,
        _ => $"{subscriptionError}\n{operationMessage}"
    };
    internal ClashOperation Operation { get; private set; }
    public bool Busy => Operation != ClashOperation.Idle;
    public bool Listening => Settings.Enabled && engine?.Alive == true;
    public event Action? Changed;

    private readonly ISettingsService? hostSettings;
    private readonly IEventAggregator? events;
    private DispatcherTimer? settingsTimer;

    public ClashService(ISettingsService settings, IEventAggregator events) : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Digital.Workstation", "Clash"), coreFolder: Path.GetDirectoryName(typeof(ClashPlugin).Assembly.Location))
    {
        hostSettings = settings;
        this.events = events;
    }
    internal ClashService(string directory, Uri? delayTarget = null, string? coreFolder = null)
    {
        this.directory = directory;
        this.coreFolder = coreFolder ?? directory;
        this.delayTarget = delayTarget ?? new Uri("https://www.gstatic.com/generate_204");
    }

    public void Start() => _ = Run(async () =>
    {
        Directory.CreateDirectory(directory);
        if (File.Exists(SettingsPath))
            Settings = JsonSerializer.Deserialize<Preferences>(File.ReadAllText(SettingsPath)) ?? new();
        if (hostSettings != null)
        {
            if (!Settings.SettingsMigrated)
            {
                if (Settings.Port != 7897) hostSettings.Set(ClashSettings.PortId, Settings.Port.ToString());
                Save(Settings with { SettingsMigrated = true });
            }
            events!.GetEvent<SettingChangedEvent>().Subscribe(SettingChanged);
            Settings = ReadHostSettings();
        }
        if (Settings.Sources.Length > 0 && (!Settings.CoreRemoved || File.Exists(CorePath))) await Reload(Settings);
    });

    private Preferences ReadHostSettings()
    {
        var raw = hostSettings!.Get<string>(ClashSettings.PortId) ?? ClashSettings.Port;
        if (!int.TryParse(raw, out var port) || port is < 1 or > 65535)
            throw new ArgumentException(ClashResources.PortRange);
        return Settings with { Port = port };
    }

    private void SettingChanged(SettingChanged change)
    {
        if (change.SettingId != ClashSettings.PortId) return;
        if (settingsTimer == null)
        {
            settingsTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(700) };
            settingsTimer.Tick += async (_, _) =>
            {
                if (Busy) return;
                settingsTimer.Stop();
                await Run(async () =>
                {
                    var next = ReadHostSettings();
                    if (next != Settings || (Settings.Sources.Length > 0 && engine?.Alive != true)) await Change(next);
                });
            };
        }
        settingsTimer.Stop();
        settingsTimer.Start();
    }

    public void Stop()
    {
        settingsTimer?.Stop();
        events?.GetEvent<SettingChangedEvent>().Unsubscribe(SettingChanged);
        lifetime.Cancel();
        CloseEngine();
        download.Dispose();
    }

    public Task Run(Func<Task> action) => Run(action, ClashOperation.Other);

    internal async Task Run(Func<Task> action, ClashOperation operation)
    {
        if (lifetime.IsCancellationRequested) return;
        if (operation == ClashOperation.SelectingNode)
        {
            try { await operationGate.WaitAsync(lifetime.Token); }
            catch (OperationCanceledException) { return; }
        }
        else if (!operationGate.Wait(0)) return;

        try
        {
            if (lifetime.IsCancellationRequested) return;
            Operation = operation;
            operationMessage = "";
            if (operation == ClashOperation.UpdatingSubscriptions) subscriptionError = "";
            Changed?.Invoke();
            await action();
        }
        catch (Exception ex)
        {
            if (operation == ClashOperation.UpdatingSubscriptions) subscriptionError = ex.Message;
            else operationMessage = ex.Message;
        }
        finally
        {
            Operation = ClashOperation.Idle;
            operationGate.Release();
            Changed?.Invoke();
        }
    }

    private void Save(Preferences settings)
    {
        File.WriteAllText(SettingsPath + ".tmp", JsonSerializer.Serialize(settings));
        File.Move(SettingsPath + ".tmp", SettingsPath, true);
        Settings = settings;
    }

    private void CloseEngine() { engine?.Dispose(); engine = null; }

    private async Task Reload(Preferences settings)
    {
        CloseEngine();
        if (settings.Sources.Length == 0)
        {
            Save(settings with { Enabled = false, Selected = null });
            Nodes = [];
            return;
        }
        try
        {
            lifetime.Token.ThrowIfCancellationRequested();
            if (settings.CoreRemoved && !File.Exists(CorePath))
                throw new InvalidOperationException(ClashResources.DownloadFirst);
            var executable = File.Exists(CorePath) && !File.Exists(CorePath + ".sha256")
                ? CorePath : await installer.ResolveAsync(coreFolder, lifetime.Token);
            lifetime.Token.ThrowIfCancellationRequested();
            engine = new Engine(directory, settings, executable);
            var nodes = await engine.ReadAsync(settings, lifetime.Token);
            if (settings.Selected != null && nodes.Any(n => n.Key == settings.Selected))
            {
                await engine.SelectAsync(settings.Selected, lifetime.Token);
                if (settings.Enabled) await OpenPort(settings.Port);
            }
            else
            {
                if (settings.Selected != null) operationMessage = ClashResources.NodeRemoved;
                settings = settings with { Enabled = false, Selected = null };
            }
            Save(settings);
            Nodes = nodes;
        }
        catch { CloseEngine(); throw; }
    }

    private async Task OpenPort(int port)
    {
        using (var probe = new TcpListener(IPAddress.Loopback, port)) { probe.ExclusiveAddressUse = true; probe.Start(); }
        await engine!.ListenAsync(port, lifetime.Token);
    }

    internal async Task Configure(int port)
    {
        if (port is < 1 or > 65535) throw new ArgumentException(ClashResources.InvalidPort);
        await Change(Settings with { Port = port });
    }

    internal async Task InstallCore()
    {
        var previous = Settings;
        bool replacing = false;
        try
        {
            await installer.ResolveAsync(coreFolder, lifetime.Token, forceCheck: true, beforeReplace: () =>
            {
                lifetime.Token.ThrowIfCancellationRequested();
                CloseEngine();
                replacing = true;
            });
        }
        catch when (replacing && File.Exists(CorePath) && !lifetime.IsCancellationRequested)
        {
            await Reload(previous);
            throw;
        }
        if (replacing || engine?.Alive != true) await Reload(previous with { CoreRemoved = false });
    }

    internal void DeleteCore()
    {
        CloseEngine();
        Save(Settings with { CoreRemoved = true, Enabled = false });
        Nodes = [];
        File.Delete(CorePath);
        File.Delete(CorePath + ".sha256");
    }

    private async Task Change(Preferences next)
    {
        var previous = Settings;
        try { await Reload(next); }
        catch when (!lifetime.IsCancellationRequested) { await Reload(previous); throw; }
    }

    public async Task Import(string name, string url, Subscription? existing)
    {
        name = name.Trim();
        if (name.Length == 0 || !Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            throw new ArgumentException(ClashResources.InvalidSubscription);
        var entry = new Subscription(existing?.Id ?? Guid.NewGuid().ToString("N"), name, uri.AbsoluteUri, DateTimeOffset.UtcNow, Guid.NewGuid());
        var path = Path.Combine(directory, entry.CacheName);
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.UserAgent.ParseAdd("clash.meta");
        using var response = await download.SendAsync(request, lifetime.Token);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsByteArrayAsync(lifetime.Token);
        // Commit the new cache reference only after the core accepts this revision.
        var sources = existing == null ? Settings.Sources.Append(entry) : Settings.Sources.Select(s => s.Id == entry.Id ? entry : s);
        try
        {
            await File.WriteAllBytesAsync(path, content, lifetime.Token);
            await Change(Settings with { Sources = sources.ToArray() });
        }
        catch
        {
            File.Delete(path);
            throw;
        }
        if (existing != null) File.Delete(Path.Combine(directory, existing.CacheName));
    }

    public async Task RefreshAll()
    {
        var failures = new List<string>();
        foreach (var source in Settings.Sources)
        {
            lifetime.Token.ThrowIfCancellationRequested();
            try { await Import(source.Name, source.Url, source); }
            catch (Exception ex) when (!lifetime.IsCancellationRequested) { failures.Add($"{source.Name}：{ex.Message}"); }
        }
        subscriptionError = string.Join("\n", failures);
    }

    public async Task Remove(Subscription source)
    {
        subscriptionError = "";
        await Change(Settings with { Sources = Settings.Sources.Where(s => s.Id != source.Id).ToArray() });
        File.Delete(Path.Combine(directory, source.CacheName));
    }

    public async Task Select(Node node)
    {
        if (!Nodes.Any(current => current.Key == node.Key))
            throw new InvalidOperationException(ClashResources.NodeRemoved);
        if (engine?.Alive != true) throw new InvalidOperationException(ClashResources.LoadFirst);
        await engine.SelectAsync(node.Key, lifetime.Token);
        Save(Settings with { Selected = node.Key });
    }

    public async Task Toggle()
    {
        if (Settings.Selected == null) throw new InvalidOperationException(ClashResources.SelectFirst);
        if (engine?.Alive != true) { await Reload(Settings with { Enabled = true }); return; }
        var previous = Settings;
        try
        {
            if (previous.Enabled) await engine.ListenAsync(0, lifetime.Token);
            else await OpenPort(previous.Port);
            Save(previous with { Enabled = !previous.Enabled });
        }
        catch when (!lifetime.IsCancellationRequested)
        {
            try { await engine.ListenAsync(previous.Enabled ? previous.Port : 0, lifetime.Token); }
            catch { CloseEngine(); }
            throw;
        }
    }

    public async Task Measure()
    {
        if (engine?.Alive != true) throw new InvalidOperationException(ClashResources.LoadFirst);
        var snapshot = Nodes.ToArray();
        for (int index = 0; index < snapshot.Length; index += 6)
        {
            var results = await Task.WhenAll(snapshot.Skip(index).Take(6).Select(n => engine.MeasureAsync(n, delayTarget, lifetime.Token)));
            for (int i = 0; i < results.Length; i++) Nodes[index+i] = results[i];
            Changed?.Invoke();
        }
    }
}
