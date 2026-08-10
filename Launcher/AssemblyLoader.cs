using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.InteropServices;

namespace DigitalWorkstation.Launcher;

/// <summary>
///     程序集加载器：
///     - 设计时（Designer/Previewer）自动禁用
///     - 运行时：预加载关键 DLL + 分类目录动态解析
/// </summary>
public sealed class AssemblyLoader
{
    /// <summary>
    ///     程序所在目录。所有相对路径都基于它解析，
    ///     保证从任意工作目录（如用绝对路径从 home 启动）都能正确定位程序集。
    /// </summary>
    private static readonly string BaseDirectory = AppContext.BaseDirectory;

    /// <summary>
    ///     启动阶段必须加载的程序集文件（相对于程序所在目录）。
    /// </summary>
    private static readonly string[] BootRequiredAssemblyFiles =
    [
        "Libraries/Serilog/Serilog.dll",
        "Libraries/Serilog/Serilog.Sinks.Console.dll",
        "Core/DigitalWorkstation.Core.Common.dll",
        "Libraries/Avalonia/Avalonia.Base.dll",
        "Libraries/Avalonia/Avalonia.Controls.dll",
        "Libraries/Avalonia/Avalonia.dll"
    ];

    /// <summary>
    ///     基础搜索路径（动态解析时使用）。
    /// </summary>
    private static readonly Dictionary<string, int> BaseFolderPath = new()
    {
        { BaseDirectory, 0 },
        { Path.Combine(BaseDirectory, "core/"), 0 },
        { Path.Combine(BaseDirectory, "libraries/"), 1 },
        { Path.Combine(BaseDirectory, "modules/"), 0 },
        { Path.Combine(BaseDirectory, "runtimes/"), 2 }
    };

    #region Singleton

    private static readonly Lazy<AssemblyLoader> SingleInstance = new(() => new AssemblyLoader());

    private static AssemblyLoader Instance => SingleInstance.Value;

    private AssemblyLoader()
    {
    }

    #endregion

    #region public API

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
            RefreshRuntimeEnvironmentPath();

            // step 3. 注册统一解析器
            AppDomain.CurrentDomain.AssemblyResolve += Instance.ResolveAssembly;

            Instance._isInitialized = true;
        }
    }

    /// <summary>
    ///     尝试加载指定路径下的程序集。
    /// </summary>
    public static Assembly? LoadAssembly(string path, string assemblyName)
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
    public static void RefreshRuntimeEnvironmentPath()
    {
        var currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";
        var separator = Path.PathSeparator;
        var paths = currentPath.Split(separator).ToList();
        foreach (var path in Instance._searchPaths.Where(p => !paths.Contains(p)))
            paths.Add(path);
        Environment.SetEnvironmentVariable("PATH", string.Join(separator, paths));
    }

    #endregion

    #region Assembly Resolve fields

    private static readonly object SearchPathsLock = new();

    private static readonly object InitLock = new();

    private static readonly string[] SourceArray = [".dll", ".exe"];

    private readonly ConcurrentDictionary<string, Assembly> _resolvedCache = new();

    private readonly List<string> _searchPaths = [];

    private bool _isInitialized;

    private bool _nativeResolversRegistered;

    /// <summary>
    ///     检测是否处于 Avalonia 设计时（多重保险）
    /// </summary>
    private static bool IsDesignEnvironment()
    {
#if DEBUG
        return true;
#endif
#pragma warning disable CS0162 // 检测到不可到达的代码
        return false;
#pragma warning restore CS0162 // 检测到不可到达的代码
    }

    private void PreloadBootAssemblies(IEnumerable<string> files)
    {
        foreach (var f in files)
        {
            var fullPath = Path.GetFullPath(Path.Combine(BaseDirectory, f));
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
            foreach (var subPaths in BaseFolderPath
                         .Select(keyValuePair => new { keyValuePair, path = keyValuePair.Key })
                         .Where(@t => Path.Exists(@t.path))
                         .Select(@t => GetAllSubDirectories(@t.path, @t.keyValuePair.Value)))
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

    #endregion

    #region Resolve Assembly

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
            if (name == null || !name.Equals(assemblyName, StringComparison.OrdinalIgnoreCase)) continue;
            _resolvedCache[assemblyName] = asm;
            return asm;
        }

        return null;
    }

    private Assembly? ResolveAssemblyFromSearchPaths(string assemblyName)
    {
        // 缓存 native 目录，供 P/Invoke 兜底 probing 使用（dlopen 会搜索 NativeLibrary 已加载目录）
        CacheNativeDirectory(assemblyName);

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
    ///     程序集解析成功时，顺便记录同包附带的 native 资产目录（如 SkiaSharp 包
    ///     的 runtimes/osx/native/libSkiaSharp.dylib），后续 P/Invoke 可据此兜底。
    /// </summary>
    private static void CacheNativeDirectory(string assemblyName)
    {
        if (_nativeDirs.ContainsKey(assemblyName)) return;

        var folder = assemblyName.Split('.')[0];
        var packageDir = Path.Combine(BaseDirectory, "libraries/", folder);
        if (!Directory.Exists(packageDir)) return;

        foreach (var file in Directory.EnumerateFiles(packageDir, "*.*", SearchOption.AllDirectories))
        {
            if (Path.GetExtension(file) is not (".dylib" or ".so" or ".dll")) continue;
            if (file.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) &&
                !Path.GetFileName(file).Contains("native", StringComparison.OrdinalIgnoreCase) &&
                !file.Contains("/runtimes/", StringComparison.OrdinalIgnoreCase)) continue;
            _nativeDirs[assemblyName] = Path.GetDirectoryName(file)!;
            return;
        }
    }

    private static readonly ConcurrentDictionary<string, string> _nativeDirs = new();

    #endregion

    #region Resolve Native Library

    /// <summary>
    ///     native 库被归入 runtimes/&lt;rid&gt;/native/ 后默认 probing 找不到，
    ///     为 Launcher 自身程序集注册 DllImportResolver 作为兜底。
    ///     在 Avalonia Setup 之前调用是安全的：只引用自身程序集，不触发任何第三方程序集加载。
    /// </summary>
    public static void RegisterNativeResolvers()
    {
        lock (InitLock)
        {
            if (Instance._nativeResolversRegistered) return;
            NativeLibrary.SetDllImportResolver(typeof(AssemblyLoader).Assembly, ResolveNativeLibrary);
            Instance._nativeResolversRegistered = true;
        }
    }

    /// <summary>
    ///     为 SkiaSharp / HarfBuzzSharp 等第三方程序集注册 native 解析器。
    ///     必须在 Avalonia 程序集加载完毕之后调用（如 AppBuilder 构造完成后），
    ///     否则 typeof 引用会触发程序集加载而崩溃。
    /// </summary>
    public static void RegisterNativeResolversForAvalonia()
    {
        NativeLibrary.SetDllImportResolver(typeof(SkiaSharp.SKImageInfo).Assembly, ResolveNativeLibrary);
        NativeLibrary.SetDllImportResolver(typeof(HarfBuzzSharp.Blob).Assembly, ResolveNativeLibrary);
        // Avalonia 的 macOS 后端通过 MicroComRuntime 对 libAvaloniaNative 做 P/Invoke，
        // AvaloniaNativePlatform 是 internal，按名字从已加载程序集中取 Avalonia.Native 程序集
        var avaloniaNative = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Avalonia.Native");
        if (avaloniaNative != null)
            NativeLibrary.SetDllImportResolver(avaloniaNative, ResolveNativeLibrary);
    }

    /// <summary>
    ///     当前运行时标识对应的 native 目录，如 runtimes/osx/native/。
    /// </summary>
    private static readonly string NativeLibraryDir =
        Path.Combine(BaseDirectory, "runtimes/",
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win-x64" :
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx" : "linux-x64",
            "native/");

    /// <summary>
    ///     立即预加载所有 native 库并常驻。
    ///     扫描 runtimes/&lt;rid&gt;/native/ 与 libraries/ 下所有包的 runtimes 资产，
    ///     后续 P/Invoke（无论默认 probing 还是 DllImportResolver）都会命中这些已加载的 handle。
    /// </summary>
    public static void PreloadNativeLibraries()
    {
        var dirs = new List<string>();
        if (Directory.Exists(NativeLibraryDir)) dirs.Add(NativeLibraryDir);

        var librariesRoot = Path.Combine(BaseDirectory, "libraries/");
        if (Directory.Exists(librariesRoot))
            dirs.AddRange(Directory.EnumerateDirectories(librariesRoot, "native", SearchOption.AllDirectories));

        foreach (var dir in dirs.Distinct())
        foreach (var file in Directory.EnumerateFiles(dir))
        {
            if (Path.GetExtension(file) is not (".dylib" or ".so" or ".dll")) continue;
            if (!NativeLibrary.TryLoad(file, out var handle)) continue;
            // 记录原始文件名（libSkiaSharp / SkiaSharp / libAvaloniaNative 等），
            // 供 DllImportResolver 按 DllImport 名称直接命中已加载 handle
            _loadedNativeHandles[Path.GetFileNameWithoutExtension(file)] = handle;
            if (file.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                _loadedNativeHandles[Path.GetFileName(file)] = handle;
        }
    }

    private static readonly ConcurrentDictionary<string, IntPtr> _loadedNativeHandles =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     按调用的托管程序集反查其 NuGet 包附带的 native 目录并加载，
    ///     解决 native 资产被归入 libraries/&lt;包名&gt;/runtimes/&lt;rid&gt;/native/ 后
    ///     默认 probing 找不到的问题。
    /// </summary>
    private static IntPtr ResolveNativeLibrary(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        // step 1. 已预加载的 handle 直接命中
        if (_loadedNativeHandles.TryGetValue(libraryName, out var loaded))
            return loaded;

        // step 2. 程序集解析时缓存的同包 native 目录
        if (_nativeDirs.TryGetValue(assembly.GetName().Name ?? "", out var cachedDir))
        {
            var handle = TryLoadFromDir(cachedDir, libraryName);
            if (handle != IntPtr.Zero) return handle;
        }

        // step 3. runtimes/<rid>/native/
        if (Directory.Exists(NativeLibraryDir))
        {
            var handle = TryLoadFromDir(NativeLibraryDir, libraryName);
            if (handle != IntPtr.Zero) return handle;
        }

        // step 4. 兜底：在 libraries/ 下按文件名搜索（仅限当前 rid 的 runtimes 目录，避免误加载其他平台）
        var librariesRoot = Path.Combine(BaseDirectory, "libraries/");
        if (!Directory.Exists(librariesRoot)) return IntPtr.Zero;

        var ridSegment = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win-x64" :
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx" : "linux-x64";
        foreach (var pattern in NativeFilePatterns(libraryName))
        {
            var file = Directory.EnumerateFiles(librariesRoot, pattern, SearchOption.AllDirectories)
                .FirstOrDefault(f => f.Contains($"/runtimes/{ridSegment}/", StringComparison.OrdinalIgnoreCase));
            if (file != null && NativeLibrary.TryLoad(file, out var handle))
                return handle;
        }

        return IntPtr.Zero;
    }

    private static IntPtr TryLoadFromDir(string dir, string libraryName)
    {
        foreach (var file in NativeFilePatterns(libraryName))
        {
            var path = Path.Combine(dir, file);
            if (File.Exists(path) && NativeLibrary.TryLoad(path, out var handle))
                return handle;
        }

        return IntPtr.Zero;
    }

    private static string[] NativeFilePatterns(string libraryName)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return [$"{libraryName}.dll", libraryName];
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return [$"lib{libraryName}.dylib", $"{libraryName}.dylib"];
        return [$"lib{libraryName}.so", $"{libraryName}.so"];
    }

    #endregion
}