using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using DigitalWorkstation.Common.Configuration;

namespace DigitalWorkstation.Common.Tools;

public sealed class AssemblyLoader
{
    private static readonly Lazy<AssemblyLoader> _instance = new(() => new AssemblyLoader());

    private AssemblyLoader()
    {
    }

    public static AssemblyLoader Instance => _instance.Value;

    #region Public API

    /// <summary>
    ///     初始化程序集解析器并接管 AssemblyResolve 事件
    /// </summary>
    internal static void Initialize(string configPath)
    {
        // 确保单例被创建并加载配置
        Instance.LoadConfiguration(configPath);

        // 确保只注册一次事件
        if (_isInitialized)
            return;
        
        AppDomain.CurrentDomain.AssemblyResolve += Instance.ResolveAssembly;

        Instance.RefreshRuntimeEnvironmentPath();
        _isInitialized = true;
    }

    /// <summary>
    ///     添加额外的搜索路径（运行时动态添加）
    /// </summary>
    /// <param name="path">
    ///     搜索路径
    /// </param>
    public static void AddSearchPath(string path)
    {
        lock (_searchPathsLock)
            if (!Instance._searchPaths.Contains(path))
            {
                Instance._searchPaths.Add(path);
                Instance.RefreshRuntimeEnvironmentPath();
            }
    }

    /// <summary>
    ///     清除所有搜索路径
    /// </summary>
    public static void ClearSearchPaths()
    {
        lock (_searchPathsLock)
            Instance._searchPaths.Clear();
    }

    /// <summary>
    ///     加载指定程序集
    /// </summary>
    /// <param name="assemblyName">
    ///     程序集名称
    /// </param>
    public static void LoadAssembly(string assemblyName)
    {
        // 确保这个 Assembly 加载到程序中
        Assembly.Load(assemblyName);
    }

    #endregion

    #region Implementation

    private static bool _isInitialized;
    private static readonly object _searchPathsLock = new();
    private readonly List<string> _searchPaths = [];
    private readonly ConcurrentDictionary<string, Assembly> _resolvedCache = new();

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    ///     加载配置文件，将路径记录到 searchPaths 中
    /// </summary>
    private void LoadConfiguration(string configPath)
    {
        try
        {
            if (!File.Exists(configPath))
                // 如果配置文件不存在，则抛出异常
                throw new FileNotFoundException($"Configuration file not found at {configPath}");


            var json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<AssemblyConfiguration>(json, _options);
            var configAbsoluteFolder = Path.GetDirectoryName(Path.GetFullPath(configPath)) ?? string.Empty;
            config?.ToAbsolutePath(configAbsoluteFolder);
            if (config?.AssemblySearchPaths == null)
                throw new JsonException("Invalid configuration file");


            lock (_searchPathsLock)
            {
                _searchPaths.Clear();
                _searchPaths.AddRange(config.AssemblySearchPaths);
                var subList = new List<string>();
                _searchPaths.ForEach(path =>
                {
                    if (!Path.Exists(path))
                        return;
                    // 获取当前路径中的子文件夹
                    var subPaths = GetAllSubDirectories(path);
                    subList.AddRange(subPaths);
                });
                _searchPaths.AddRange(subList);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Load configuration file error.", nameof(AssemblyLoader));
        }
    }

    private List<string> GetAllSubDirectories(string path, int limit = 5)
    {
        var subDirectories = new List<string>();
        var directories = Directory.GetDirectories(path);
        subDirectories.AddRange(directories);
        if (limit == 0)
            return subDirectories;

        foreach (var directory in directories)
        {
            var subDirs = GetAllSubDirectories(directory, limit - 1);
            subDirectories.AddRange(subDirs);
        }

        return subDirectories;
    }

    public Assembly? ResolveAssembly(object? sender, ResolveEventArgs args)
    {
        var assemblyName = new AssemblyName(args.Name).Name;
        
        if (string.IsNullOrWhiteSpace(assemblyName))
            return null;

        // 忽略资源请求
        if (assemblyName.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
            return null;
        
        if (_resolvedCache.TryGetValue(assemblyName, out var cachedAssembly))
            return cachedAssembly;


        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var name = asm.GetName().Name;
            if (name == null || !name.Equals(assemblyName, StringComparison.OrdinalIgnoreCase))
                continue;
            
            _resolvedCache[assemblyName] = asm;
            return asm;
        }

        var folder = assemblyName.Split('.')[0];
        var targetFolder = _searchPaths.FirstOrDefault(x => x.Contains(folder));

        if (targetFolder != null)
        {
            var result = LoadAssembly(targetFolder, assemblyName);
            if (result != null)
            {
                _resolvedCache[assemblyName] = result;
                return result;
            }
        }

        foreach (var path in _searchPaths)
        {
            var result = LoadAssembly(path, assemblyName);
            if (result != null)
            {
                _resolvedCache[assemblyName] = result;
                return result;
            }
        }

        // 4. 记录未找到信息
        Logger.Warning($"Failed to resolve: {assemblyName}", nameof(AssemblyLoader));
        Logger.Warning("Search paths:", nameof(AssemblyLoader));
        foreach (var path in _searchPaths)
            Logger.Warning($"\t - {Path.GetFullPath(path)}", nameof(AssemblyLoader));


        return null;
    }

    private Assembly? LoadAssembly(string path, string assemblyName)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            if (!Directory.Exists(fullPath))
                return null;

            var extensions = new[] { ".dll", ".exe" };
            return (from ext in extensions
                select Path.Combine(fullPath, $"{assemblyName}{ext}")
                into assemblyPath
                where File.Exists(assemblyPath)
                select Assembly.LoadFrom(assemblyPath)).FirstOrDefault();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Error when load {path}.", nameof(AssemblyLoader));
            return null;
        }
    }

    private void RefreshRuntimeEnvironmentPath()
    {
        var currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";
        var paths = currentPath.Split(';')
            .ToList();
        foreach (var path in _searchPaths.Where(path => !paths.Contains(path)))
            paths.Add(path);

        Environment.SetEnvironmentVariable("PATH", string.Join(";", paths));
    }

    #endregion
}