using System.Collections.Concurrent;
using System.Reflection;
using System.Security;

namespace DigitalWorkstation.Launcher.Utils;

public static class AssemblyLoader
{
    private static readonly string[] _basePaths = [@".\", @".\Core", @".\Modules", @".\Libraries"];
    private static readonly List<string> _searchPaths = [];
    private static readonly ConcurrentDictionary<string, Assembly> _resolvedCache = new();
    private static readonly string[] _validExtensions = [".dll", ".exe"];

    public static void Initialize()
    {
        _searchPaths.Clear();
        foreach (var path in _basePaths)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                continue;
            _searchPaths.Add(path);
            _searchPaths.AddRange(GetSubdirectories(path));
        }

        Logger.Information(nameof(AssemblyLoader), $"Search path initialized with {_searchPaths.Count} entries");

        AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
        Logger.Information(nameof(AssemblyLoader), "AssemblyResolve event registered");
    }

    private static List<string> GetSubdirectories(string path, int limit = 5)
    {
        if (string.IsNullOrWhiteSpace(path) || limit <= 0 || !Directory.Exists(path))
        {
            return [];
        }

        var result = new List<string>();
        try
        {
            var directories = Directory.GetDirectories(path);
            foreach (var directory in directories)
            {
                if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                    continue;
                result.Add(directory);
                result.AddRange(GetSubdirectories(directory, limit - 1));
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.Warning(nameof(AssemblyLoader), $"Failed to access directory {path}: {ex.Message}");
        }
        catch (DirectoryNotFoundException)
        {
            Logger.Warning(nameof(AssemblyLoader), $"Directory {path} not found");
        }
        catch (SecurityException ex)
        {
            Logger.Warning(nameof(AssemblyLoader), $"Security error accessing directory {path}: {ex.Message}");
        }

        return result;
    }

    private static Assembly? ResolveAssembly(object? sender, ResolveEventArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.Name))
            return null;

        var assemblyName = new AssemblyName(args.Name).Name;

        // 资源请求检查 - 只处理非资源文件
        if (assemblyName?.EndsWith(".resources", StringComparison.OrdinalIgnoreCase) == true)
            return null;

        // 检查缓存
        if (assemblyName != null && _resolvedCache.TryGetValue(assemblyName, out var cachedAssembly))
            return cachedAssembly;

        // 检查已加载的程序集
        var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(asm => asm.GetName()
                                       .Name?.Equals(assemblyName, StringComparison.OrdinalIgnoreCase) ==
                                   true);

        if (loadedAssembly != null)
        {
            if (assemblyName != null)
                _resolvedCache[assemblyName] = loadedAssembly;
            return loadedAssembly;
        }

        // 在搜索路径中查找程序集
        foreach (var path in _searchPaths.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            try
            {
                var fullPath = Path.GetFullPath(path);
                if (!Directory.Exists(fullPath))
                    continue;

                foreach (var ext in _validExtensions)
                {
                    if (string.IsNullOrWhiteSpace(ext))
                        continue;
                    var assemblyPath = Path.Combine(fullPath, $"{assemblyName}{ext}");
                    if (!File.Exists(assemblyPath))
                        continue;

                    try
                    {
                        var assembly = Assembly.LoadFrom(assemblyPath);
                        if (assemblyName != null)
                            _resolvedCache[assemblyName] = assembly;
                        return assembly;
                    }
                    catch (BadImageFormatException)
                    {
                        // 忽略不是程序集文件的DLL
                    }
                    catch (FileLoadException ex)
                    {
                        Logger.Error(nameof(AssemblyLoader), $"Failed to load assembly from {assemblyPath}.", ex);
                    }
                }
            }
            catch (Exception ex) when (ex is ArgumentException or PathTooLongException or NotSupportedException)
            {
                Logger.Error(nameof(AssemblyLoader), $"Error processing path {path}.", ex);
            }
        }

        Logger.Warning(nameof(AssemblyLoader), $"Failed to resolve: {assemblyName}");
        Logger.Warning(nameof(AssemblyLoader), "Search paths:");
        foreach (var path in _searchPaths)
            Logger.Warning(nameof(AssemblyLoader), $"\t - {Path.GetFullPath(path)}");

        return null;
    }
}
