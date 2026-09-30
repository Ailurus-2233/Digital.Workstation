using System.Text.Json;
using DigitalWorkstation.Core.Common;
using DigitalWorkstation.Core.Services.Persistence;

namespace DigitalWorkstation.Core.Services.Plugins;

/// <summary>插件启动选择与本次启动快照；修改只在下次启动生效。</summary>
public sealed class PluginStartupService
{
    public static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Digital.Workstation", "plugins.json");

    private static readonly string RemovalPath = Path.Combine(Path.GetDirectoryName(FilePath)!, "plugin-removals.json");
    private readonly HashSet<string> _removals = new(StringComparer.OrdinalIgnoreCase);
    private readonly DebouncedJsonFile<string[]> _removalFile;
    private readonly Lock _gate = new();
    private readonly Dictionary<string, bool> _values = new(StringComparer.Ordinal);
    private readonly Dictionary<string, bool> _startupValues = new(StringComparer.Ordinal);
    private readonly DebouncedJsonFile<Dictionary<string, bool>> _file;
    private readonly Dictionary<string, PluginStartupInfo> _plugins = new(StringComparer.Ordinal);

    public PluginStartupService(ConfigurationPersistence persistence)
    {
        _removalFile = persistence.CreateFile<string[]>(RemovalPath, new JsonSerializerOptions { WriteIndented = true });
        _file = persistence.CreateFile<Dictionary<string, bool>>(FilePath,
            new JsonSerializerOptions { WriteIndented = true });
    }

    internal void Load()
    {
        ProcessRemovals();
        try
        {
            if (!File.Exists(FilePath)) return;
            var values = JsonSerializer.Deserialize<Dictionary<string, bool>>(File.ReadAllText(FilePath))
                ?? throw new JsonException("Plugin startup configuration must be an object.");
            foreach (var (id, enabled) in values)
            {
                _values[id] = enabled;
                _startupValues[id] = enabled;
            }
        }
        catch (Exception exception)
        {
            Logger.Warning($"Failed to read plugin startup configuration ({exception.GetType().Name}); using default values: {FilePath}",
                nameof(PluginStartupService));
        }
    }

    public IReadOnlyList<PluginStartupInfo> Plugins
    {
        get
        {
            lock (_gate) return _plugins.Values.OrderBy(plugin => plugin.Id, StringComparer.Ordinal).ToArray();
        }
    }

    public bool IsEnabled(string id)
    {
        lock (_gate) return _values.GetValueOrDefault(id, true);
    }

    internal bool IsEnabledAtStartup(string id)
    {
        lock (_gate) return _startupValues.GetValueOrDefault(id, true);
    }

    public bool IsPendingRestart(string id)
    {
        lock (_gate) return _values.GetValueOrDefault(id, true) != _startupValues.GetValueOrDefault(id, true);
    }

    public void SetEnabled(string id, bool enabled)
    {
        lock (_gate)
        {
            if (!_plugins.ContainsKey(id)) throw new ArgumentException("Unknown plugin identity.", nameof(id));
            _values[id] = enabled;
            _file.ScheduleSave(new Dictionary<string, bool>(_values, StringComparer.Ordinal));
        }
    }

    internal void MigrateIdentity(string entryName, string key)
    {
        lock (_gate)
        {
            if (key == entryName || !_values.Remove(entryName, out var enabled)) return;
            _values.TryAdd(key, enabled);
            if (_startupValues.Remove(entryName, out var startup)) _startupValues.TryAdd(key, startup);
            _file.ScheduleSave(new Dictionary<string, bool>(_values, StringComparer.Ordinal));
        }
    }

    public bool IsRemovalPending(string id)
    {
        lock (_gate) return _plugins.TryGetValue(id, out var plugin) && _removals.Contains(plugin.AssemblyPath);
    }

    /// <summary>安排下次启动前卸载文件；当前进程保留已经加载的插件。</summary>
    public void Remove(string id)
    {
        lock (_gate)
        {
            if (!_plugins.TryGetValue(id, out var plugin)) throw new ArgumentException("Unknown plugin identity.", nameof(id));
            ValidateRemovalPath(plugin.AssemblyPath);
            _removals.Add(plugin.AssemblyPath);
            _removalFile.ScheduleSave(_removals.ToArray());
        }
    }

    private static string ValidateRemovalPath(string assemblyPath)
    {
        var path = Path.GetFullPath(assemblyPath);
        var basePath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(AppContext.BaseDirectory));
#if DEBUG
        if (!string.Equals(Path.GetDirectoryName(path), basePath, StringComparison.OrdinalIgnoreCase) ||
            !path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only plugin assemblies in the application directory can be removed.");
        return path;
#else
        var pluginsRoot = Path.Combine(basePath, "plugins");
        var directory = Path.GetDirectoryName(path)!;
        if (!string.Equals(Path.GetDirectoryName(directory), pluginsRoot, StringComparison.OrdinalIgnoreCase) ||
            !path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only isolated plugin directories can be removed.");
        return directory;
#endif
    }

    private void ProcessRemovals()
    {
        try
        {
            if (!File.Exists(RemovalPath)) return;
            foreach (var path in JsonSerializer.Deserialize<string[]>(File.ReadAllText(RemovalPath)) ?? [])
            {
                try
                {
                    var target = ValidateRemovalPath(path);
                    if (File.Exists(path) && !PluginDiscovery.IsPluginAssembly(path))
                        throw new IOException("Removal target is not a plugin assembly.");
#if !DEBUG
                    var parent = Directory.GetParent(target)!;
                    if (parent.Exists && (parent.Attributes & FileAttributes.ReparsePoint) != 0)
                        throw new IOException("Plugin root must not be a link.");
#endif
                    // 不跟随链接删除；只允许应用当前发布目录内的插件。
                    if (File.Exists(target) && (File.GetAttributes(target) & FileAttributes.ReparsePoint) != 0)
                        throw new IOException("Plugin removal target must not be a link.");
#if DEBUG
                    File.Delete(target);
#else
                    if (Directory.Exists(target))
                    {
                        var directory = new DirectoryInfo(target);
                        if ((directory.Attributes & FileAttributes.ReparsePoint) != 0 ||
                            directory.EnumerateFileSystemInfos("*", SearchOption.AllDirectories)
                                .Any(entry => (entry.Attributes & FileAttributes.ReparsePoint) != 0))
                            throw new IOException("Plugin removal directory must not contain links.");
                        Directory.Delete(target, recursive: true);
                    }
#endif
                    Logger.Information("Plugin files removed: " + target, nameof(PluginStartupService));
                }
                catch (Exception exception)
                {
                    _removals.Add(path);
                    Logger.Warning("Failed to remove plugin files: " + exception.Message, nameof(PluginStartupService));
                }
            }
            _removalFile.ScheduleSave(_removals.ToArray());
        }
        catch (Exception exception)
        {
            Logger.Warning("Failed to read pending plugin removals: " + exception.Message, nameof(PluginStartupService));
        }
    }
    internal void Record(PluginDescriptor plugin, bool loaded = false)
    {
        lock (_gate)
        {
            _plugins[plugin.Name] = new PluginStartupInfo(plugin.Name, plugin.AssemblyPath,
                _startupValues.GetValueOrDefault(plugin.Name, true), loaded, plugin.Error?.Message);
        }
    }
}
