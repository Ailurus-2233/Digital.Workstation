using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace DigitalWorkstation.Core.Framework.Plugins;

/// <summary>Release 每插件独立解析私有依赖；共享契约返回宿主已经使用的实际程序集实例。</summary>
internal sealed class PluginLoadContext : AssemblyLoadContext
{
    // Launcher uses this prefix to keep its global resolver outside plugin contexts.
    internal const string ContextNamePrefix = "DigitalWorkstation.Plugin:";
    private const string SharedResourceName = "DigitalWorkstation.Core.Framework.Plugins.SharedAssemblies";
    private static readonly string[] SharedPatterns = ReadSharedPatterns();
    private static readonly HashSet<string> RuntimeAssemblies = Directory
        .EnumerateFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll")
        .Select(Path.GetFileNameWithoutExtension).OfType<string>().ToHashSet(StringComparer.OrdinalIgnoreCase);
    private readonly string _directory;
    private readonly AssemblyDependencyResolver _resolver;
    private readonly ConcurrentDictionary<string, Assembly> _shared = new(StringComparer.OrdinalIgnoreCase);

    internal PluginLoadContext(string assemblyPath, IReadOnlyCollection<Assembly> sharedModules)
        : base(ContextNamePrefix + Path.GetFileNameWithoutExtension(assemblyPath), isCollectible: false)
    {
        _directory = Path.GetDirectoryName(Path.GetFullPath(assemblyPath))!;
        _resolver = new AssemblyDependencyResolver(assemblyPath);
        foreach (var assembly in sharedModules)
            _shared.TryAdd(assembly.GetName().Name!, assembly);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var name = assemblyName.Name ?? throw new FileLoadException("The assembly name is empty.");
        if (_shared.TryGetValue(name, out var shared)) return shared;
        if (RuntimeAssemblies.Contains(name)) return Default.LoadFromAssemblyName(assemblyName);
        if (SharedPatterns.Any(pattern => Matches(name, pattern)))
            return _shared.GetOrAdd(name, _ => ResolveHostAssembly(assemblyName));

        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        if (path is null)
        {
            // Unlisted local references and satellite resources may not occur in the deps manifest.
            path = string.IsNullOrEmpty(assemblyName.CultureName)
                ? Path.Combine(_directory, name + ".dll")
                : Path.Combine(_directory, assemblyName.CultureName, name + ".dll");
        }
        EnsurePluginPath(path);
        if (File.Exists(path)) return LoadFromAssemblyPath(path);

        // FileLoadException stops default probing; ResourceManager catches it for satellite culture fallback.
        throw new FileLoadException($"Private plugin assembly {assemblyName.FullName} was not found in {_directory}.", path);
    }

    protected override nint LoadUnmanagedDll(string unmanagedDllName)
    {
        // System APIs may use absolute paths, including macOS libraries supplied only by the dyld cache.
        if (Path.IsPathRooted(unmanagedDllName) &&
            SystemNativeDirectories().Any(directory => IsWithinDirectory(directory, unmanagedDllName)))
        {
            if (NativeLibrary.TryLoad(unmanagedDllName, out var systemHandle)) return systemHandle;
            throw new DllNotFoundException($"System native library {unmanagedDllName} could not be loaded.");
        }

        var path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (path is not null)
        {
            EnsurePluginPath(path);
            return LoadUnmanagedDllFromPath(path);
        }
        foreach (var file in NativeFileNames(unmanagedDllName))
        {
            path = Path.Combine(_directory, file);
            EnsurePluginPath(path);
            if (File.Exists(path)) return LoadUnmanagedDllFromPath(path);
        }

        // OS libraries remain available without allowing application-root or other-plugin probing.
        foreach (var directory in SystemNativeDirectories())
        foreach (var file in NativeFileNames(unmanagedDllName))
        {
            path = Path.Combine(directory, file);
            if (NativeLibrary.TryLoad(path, out var handle)) return handle;
        }
        throw new DllNotFoundException($"Private plugin native library {unmanagedDllName} was not found in {_directory}.");
    }

    private void EnsurePluginPath(string path)
    {
        if (!IsWithinDirectory(_directory, path))
            throw new FileLoadException($"Plugin dependency resolved outside its directory: {path}");
    }

    private static bool IsWithinDirectory(string directory, string path)
    {
        var relative = Path.GetRelativePath(directory, Path.GetFullPath(path));
        return !Path.IsPathRooted(relative) && relative != ".." &&
               !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal);
    }

    private static Assembly ResolveHostAssembly(AssemblyName name)
    {
        var loaded = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly =>
            GetLoadContext(assembly) is not PluginLoadContext &&
            string.Equals(assembly.GetName().Name, name.Name, StringComparison.OrdinalIgnoreCase));
        // Launcher may preload host assemblies with LoadFile; reuse them to preserve contract identities.
        return loaded ?? Default.LoadFromAssemblyName(name);
    }

    private static bool Matches(string name, string pattern) => pattern.EndsWith('*')
        ? name.StartsWith(pattern[..^1], StringComparison.OrdinalIgnoreCase)
        : name.Equals(pattern, StringComparison.OrdinalIgnoreCase);

    private static string[] ReadSharedPatterns()
    {
        using var stream = typeof(PluginLoadContext).Assembly.GetManifestResourceStream(SharedResourceName)
            ?? throw new InvalidOperationException("The shared plugin assembly manifest is missing.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => !line.StartsWith('#')).ToArray();
    }

    private static IEnumerable<string> NativeFileNames(string name)
    {
        yield return name;
        if (OperatingSystem.IsWindows())
        {
            if (!name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) yield return name + ".dll";
        }
        else
        {
            var suffix = OperatingSystem.IsMacOS() ? ".dylib" : ".so";
            if (!name.EndsWith(suffix, StringComparison.Ordinal)) yield return name + suffix;
            if (!name.StartsWith("lib", StringComparison.Ordinal))
                yield return "lib" + name + (name.EndsWith(suffix, StringComparison.Ordinal) ? "" : suffix);
        }
    }

    private static IEnumerable<string> SystemNativeDirectories()
    {
        if (OperatingSystem.IsWindows())
        {
            yield return Environment.SystemDirectory;
            yield break;
        }
        yield return "/usr/lib";
        yield return "/lib";
        if (OperatingSystem.IsLinux())
        {
            var architecture = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "x86_64-linux-gnu",
                Architecture.Arm64 => "aarch64-linux-gnu",
                Architecture.X86 => "i386-linux-gnu",
                _ => ""
            };
            if (architecture.Length > 0)
            {
                yield return "/usr/lib/" + architecture;
                yield return "/lib/" + architecture;
            }
            yield return "/usr/lib64";
            yield return "/lib64";
        }
    }
}
