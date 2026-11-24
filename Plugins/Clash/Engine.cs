using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;

namespace DigitalWorkstation.Clash;

public sealed record Subscription(string Id, string Name, string Url, DateTimeOffset? UpdatedAt = null, Guid? Revision = null)
{
    internal string CacheName => Revision is { } revision ? $"{Id}-{revision:N}.yaml" : $"{Id}.yaml";
    public override string ToString() => Name;
}

public sealed record Preferences
{
    public bool SettingsMigrated { get; init; }
    public int Port { get; init; } = 7897;
    public bool CoreRemoved { get; init; }
    public Subscription[] Sources { get; init; } = [];
    public string? Selected { get; init; }
    public bool Enabled { get; init; }
}

public enum NodeState { Untested, Available, TimedOut, Failed }

internal sealed record StateFilter(NodeState? State)
{
    public bool Matches(Node node) => State == null || node.State == State;
    public override string ToString() => ClashResources.Text(State?.ToString() ?? "All");
}

public sealed record Node(Subscription Source, string Key, string Name, NodeState State = NodeState.Untested, int? Delay = null)
{
    public string Updated => Source.UpdatedAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "—";
    public string Latency => Delay.HasValue ? $"{Delay} ms" : ClashResources.Text(State.ToString());
    public override string ToString() => $"{Name} · {Source.Name} · {Latency}";
}

internal sealed class Engine : IDisposable
{
    private readonly HttpClient api = new(new HttpClientHandler { UseProxy = false });
    private readonly Process process;
    internal bool Alive => !process.HasExited;

    internal Engine(string directory, Preferences settings, string executable)
    {
        using var socket = new TcpListener(IPAddress.Loopback, 0);
        socket.Start();
        var controller = ((IPEndPoint)socket.LocalEndpoint).Port;
        var secret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var providers = settings.Sources.ToDictionary(s => s.Id, s => new Dictionary<string,object> {
            ["type"] = "file", ["path"] = s.CacheName,
            ["override"] = new Dictionary<string,string> { ["additional-prefix"] = s.Id + "-" }
        });
        var config = new Dictionary<string,object> {
            ["external-controller"] = $"127.0.0.1:{controller}", ["secret"] = secret,
            ["allow-lan"] = false, ["mode"] = "rule", ["proxy-providers"] = providers,
            ["proxy-groups"] = new[] { new { name = "Selected", type = "select", proxies = new[]{"REJECT"}, use = providers.Keys.ToArray() } },
            ["rules"] = new[]{"MATCH,Selected"}, ["profile"] = new Dictionary<string,bool> { ["store-selected"] = false }
        };
        string path = Path.Combine(directory, "core.json");
        File.WriteAllText(path, JsonSerializer.Serialize(config));
        var start = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true,
            WorkingDirectory = directory, ArgumentList = { "-d", directory, "-f", path } };
        socket.Stop();
        process = Process.Start(start) ?? throw new IOException(ClashResources.StartFailed);
        api.BaseAddress = new Uri($"http://127.0.0.1:{controller}/");
        api.DefaultRequestHeaders.Authorization = new("Bearer", secret);
    }

    internal async Task<List<Node>> ReadAsync(Preferences config, CancellationToken token)
    {
        for (int i = 0; ; i++)
        {
            if (!Alive) throw new IOException(ClashResources.Exited);
            try { using var ready = await api.GetAsync("version", token); ready.EnsureSuccessStatusCode(); break; }
            catch (HttpRequestException) when (i < 60) { await Task.Delay(100, token); }
        }
        List<Node> nodes = [];
        foreach (var source in config.Sources)
        {
            var provider = await api.GetFromJsonAsync<JsonElement>("providers/proxies/" + source.Id, token);
            foreach (var item in provider.GetProperty("proxies").EnumerateArray())
            {
                var key = item.GetProperty("name").GetString()!;
                if (key.StartsWith(source.Id + "-", StringComparison.Ordinal))
                    nodes.Add(new(source, key, key[(source.Id.Length + 1)..]));
            }
            if (!nodes.Any(n => n.Source == source)) throw new InvalidDataException(string.Format(ClashResources.EmptySource, source.Name));
        }
        return nodes;
    }

    internal async Task SelectAsync(string key, CancellationToken token)
    {
        using var response = await api.PutAsJsonAsync("proxies/Selected", new { name = key }, token);
        response.EnsureSuccessStatusCode();
    }

    internal async Task ListenAsync(int port, CancellationToken token)
    {
        using var response = await api.PatchAsJsonAsync("configs", new Dictionary<string,int> { ["mixed-port"] = port }, token);
        response.EnsureSuccessStatusCode();
        if (port > 0)
        {
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, port, token);
            // Verify a SOCKS handshake since a conflicting service may own this port.
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            timeout.CancelAfter(TimeSpan.FromSeconds(2));
            var stream = client.GetStream();
            await stream.WriteAsync(new byte[]{5,1,0}, timeout.Token);
            byte[] reply = new byte[2];
            await stream.ReadExactlyAsync(reply, timeout.Token);
            if (reply[0] != 5 || reply[1] != 0) throw new IOException(ClashResources.PortUnavailable);
        }
    }

    internal async Task<Node> MeasureAsync(Node node, Uri target, CancellationToken token)
    {
        try
        {
            var path = $"providers/proxies/{node.Source.Id}/{Uri.EscapeDataString(node.Key)}/healthcheck?timeout=5000&url={Uri.EscapeDataString(target.AbsoluteUri)}";
            using var response = await api.GetAsync(path, token);
            if (response.StatusCode == HttpStatusCode.GatewayTimeout) return node with { State = NodeState.TimedOut, Delay = null };
            response.EnsureSuccessStatusCode();
            var data = await response.Content.ReadFromJsonAsync<JsonElement>(token);
            return node with { State = NodeState.Available, Delay = data.GetProperty("delay").GetInt32() };
        }
        catch (HttpRequestException) { return node with { State = NodeState.Failed, Delay = null }; }
    }

    public void Dispose()
    {
        api.Dispose();
        try { if (!process.HasExited) { process.Kill(true); process.WaitForExit(); } }
        finally { process.Dispose(); }
    }
}
