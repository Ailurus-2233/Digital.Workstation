using System.Collections.Concurrent;
using System.Reflection;

namespace DigitalWorkstation.Launcher;

/// <summary>
///     程序集加载器：
///     - 设计时（Designer/Previewer）自动禁用
///     - 运行时：预加载关键 DLL + 分类目录动态解析
/// </summary>
public sealed class AssemblyLoader
{
    /// <summary>
    ///     启动阶段必须加载的程序集文件（绝对路径 / 相对路径都行）。
    /// </summary>
    private static readonly string[] BootRequiredAssemblyFiles =
    [
        "./Libraries/Serilog/Serilog.dll",
        "./Libraries/Serilog/Serilog.Sinks.Console.dll",
        "./Core/DigitalWorkstation.Common.dll"
    ];

    /// <summary>
    ///     基础搜索路径（动态解析时使用）。
    /// </summary>
    private static readonly Dictionary<string, int> BaseFolderPath = new()
    {
        { Path.GetFullPath("./"), 0 },
        { Path.GetFullPath("./core/"), 0 },
        { Path.GetFullPath("./libraries/"), 1 },
        { Path.GetFullPath("./modules/"), 0 },
        { Path.GetFullPath("./runtimes/"), 2 }
    };

    private static readonly object InitLock = new();

    /// <summary>
    ///     单例。
    /// </summary>
    private static readonly Lazy<AssemblyLoader> SingleInstance = new(() => new AssemblyLoader());

    private static readonly object SearchPathsLock = new();


    private static readonly string[] SourceArray = [".dll", ".exe"];

    /// <summary>
    ///     已解析缓存。
    /// </summary>
    private readonly ConcurrentDictionary<string, Assembly> _resolvedCache = new();

    /// <summary>
    ///     搜索路径。
    /// </summary>
    private readonly List<string> _searchPaths = [];

    /// <summary>
    ///     是否已初始化。
    /// </summary>
    private bool _isInitialized;

    private AssemblyLoader()
    {
    }

    public static AssemblyLoader Instance => SingleInstance.Value;

    /// <summary>
    ///     初始化：预加载必要程序集 + 注册动态解析。
    /// </summary>
    public static void Initialize()
    {
        lock (InitLock)
        {
            if (Instance._isInitialized) return;

            // 设计时禁用：不做任何事，保证预览器能直接从输出根目录探测到 DLL
            if (IsDesignEnvironment())
            {
                Instance._isInitialized = true;
                return;
            }

            // step 1. 预加载关键 DLL（避免 StackOverflowException）
            Instance.PreloadBootAssemblies(BootRequiredAssemblyFiles);

            // step 2. 初始化搜索路径
            Instance.InitializeSearchPath();
            Instance.RefreshRuntimeEnvironmentPath();

            // step 3. 注册统一解析器
            AppDomain.CurrentDomain.AssemblyResolve += Instance.ResolveAssembly;

            Instance._isInitialized = true;
        }
    }

    /// <summary>
    ///     检测是否处于 Avalonia 设计时（多重保险）
    /// </summary>
    private static bool IsDesignEnvironment()
    {
        try
        {
            // 1) Avalonia 官方设计时检测（若存在引用）
            //    直接反射以避免你在非 UI 层硬引用 Avalonia
            var avaloniaControls = AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Avalonia.Controls");
            if (avaloniaControls != null)
            {
                var designType = avaloniaControls.GetType("Avalonia.Controls.Design");
                var isDesignProp = designType?.GetProperty("IsDesignMode", BindingFlags.Public | BindingFlags.Static);
                if (isDesignProp?.GetValue(null) is true) return true;
            }
        }
        catch
        {
            /* ignore */
        }
        return false;
    }

    private void PreloadBootAssemblies(IEnumerable<string> files)
    {
        foreach (var f in files)
        {
            var fullPath = Path.GetFullPath(f);
            if (!File.Exists(fullPath)) continue;
            // 用 LoadFile（不是 LoadFrom）避免 probing/AssemblyResolve 递归
            var assembly = Assembly.LoadFile(fullPath);
            var name = assembly.GetName().Name;
            if (name != null)
                _resolvedCache[name] = assembly;
        }
    }

    /// <summary>
    ///     初始化搜索路径。
    /// </summary>
    private void InitializeSearchPath()
    {
        lock (SearchPathsLock)
        {
            _searchPaths.Clear();
            _searchPaths.AddRange(BaseFolderPath.Keys);

            // 附带子目录（递归深度 2）
            var subList = new List<string>();
            foreach (var subPaths in from keyValuePair in BaseFolderPath
                     let path = keyValuePair.Key
                     where Path.Exists(path)
                     select GetAllSubDirectories(path, keyValuePair.Value))
                subList.AddRange(subPaths);

            _searchPaths.AddRange(subList);
        }
    }

    /// <summary>
    ///     递归获取子目录。
    /// </summary>
    private static List<string> GetAllSubDirectories(string path, int limit = 4)
    {
        var subDirectories = new List<string>();
        if (limit == 0) return subDirectories;

        var directories = Directory.GetDirectories(path);
        subDirectories.AddRange(directories);

        foreach (var directory in directories)
        {
            var subDirs = GetAllSubDirectories(directory, limit - 1);
            subDirectories.AddRange(subDirs);
        }

        return subDirectories;
    }


    /// <summary>
    ///     动态程序集解析。
    /// </summary>
    private Assembly? ResolveAssembly(object? sender, ResolveEventArgs args)
    {
        var assemblyName = new AssemblyName(args.Name).Name;
        if (string.IsNullOrWhiteSpace(assemblyName)) return null;
        if (assemblyName.EndsWith(".resources", StringComparison.OrdinalIgnoreCase)) return null;

        // step 1. 缓存
        var result = ResolveAssemblyFromCache(assemblyName);
        if (result != null) return result;

        // step 2. 已加载程序集
        result = ResolveAssemblyFromLoaded(assemblyName);
        if (result != null) return result;

        // step 3. 搜索路径
        result = ResolveAssemblyFromSearchPaths(assemblyName);
        return result != null ? result : null;
    }

    private Assembly? ResolveAssemblyFromCache(string assemblyName)
    {
        return _resolvedCache.GetValueOrDefault(assemblyName);
    }

    private Assembly? ResolveAssemblyFromLoaded(string assemblyName)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var name = asm.GetName().Name;
            if (name == null || 
                !name.Equals(assemblyName, StringComparison.OrdinalIgnoreCase)) continue;
            _resolvedCache[assemblyName] = asm;
            return asm;
        }

        return null;
    }
    
    private Assembly? ResolveAssemblyFromSearchPaths(string assemblyName)
    {
        var folder = assemblyName.Split('.')[0];
        var targetFolder = _searchPaths.FirstOrDefault(x => x.Contains(folder, StringComparison.OrdinalIgnoreCase));
        if (targetFolder != null)
        {
            var asm = LoadAssembly(targetFolder, assemblyName);
            if (asm != null)
            {
                _resolvedCache[assemblyName] = asm;
                return asm;
            }
        }
        
        foreach (var asm in _searchPaths.Select(path => LoadAssembly(path, assemblyName)).OfType<Assembly>())
        {
            _resolvedCache[assemblyName] = asm;
            return asm;
        }

        return null;
    }

    /// <summary>
    ///     尝试加载指定路径下的程序集。
    /// </summary>
    private Assembly? LoadAssembly(string path, string assemblyName)
    {
        var fullPath = Path.GetFullPath(path);
        if (!Directory.Exists(fullPath)) return null;

        return (from ext in SourceArray
            select Path.Combine(fullPath, $"{assemblyName}{ext}")
            into assemblyPath
            where File.Exists(assemblyPath)
            select Assembly.LoadFrom(assemblyPath)).FirstOrDefault();
    }

    /// <summary>
    ///     刷新 PATH 环境变量，支持本地依赖的 native 库。
    /// </summary>
    private void RefreshRuntimeEnvironmentPath()
    {
        var currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";
        var paths = currentPath.Split(';').ToList();
        foreach (var path in _searchPaths.Where(p => !paths.Contains(p)))
            paths.Add(path);
        Environment.SetEnvironmentVariable("PATH", string.Join(";", paths));
    }
}