using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.Loader;
using DigitalWorkstation.Core.Abstractions.Plugins;

namespace DigitalWorkstation.Core.Framework.Plugins;

/// <summary>先读取 PE 元数据筛选入口并检查冲突，再加载候选；普通依赖和 native DLL 不进入模块生命周期。</summary>
internal static class PluginDiscovery
{
    public static IReadOnlyList<PluginDescriptor> Discover(IReadOnlyCollection<Assembly> sharedModules)
    {
        var result = new List<PluginDescriptor>();
        var candidates = new List<Candidate>();
#if DEBUG
        ScanDirectory(AppContext.BaseDirectory, false, candidates, result);
#else
        var root = Path.Combine(AppContext.BaseDirectory, "plugins");
        if (!Directory.Exists(root)) return result;
        try
        {
            foreach (var directory in Directory.GetDirectories(root).Order(StringComparer.Ordinal))
                ScanDirectory(directory, true, candidates, result);
        }
        catch (Exception ex)
        {
            result.Add(Failure(root, ex));
        }
#endif
        var duplicateNames = candidates.GroupBy(candidate => candidate.Name, StringComparer.Ordinal)
            .Where(group => group.Count() > 1).Select(group => group.Key).ToHashSet(StringComparer.Ordinal);
        var duplicateAssemblies = candidates.GroupBy(candidate => candidate.AssemblyName, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1).Select(group => group.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in candidates)
        {
            if (duplicateNames.Contains(candidate.Name) || duplicateAssemblies.Contains(candidate.AssemblyName))
            {
                result.Add(Failure(candidate.Path,
                    new InvalidOperationException($"Duplicate plugin entry or assembly identity: {candidate.Name} ({candidate.AssemblyName}).")));
                continue;
            }
            try
            {
#if DEBUG
                var context = AssemblyLoadContext.Default;
#else
                var context = new PluginLoadContext(candidate.Path, sharedModules);
#endif
                var assembly = context.LoadFromAssemblyPath(candidate.Path);
                var type = assembly.GetType(candidate.Name, throwOnError: true)!;
                if (!type.IsClass || !type.IsVisible || type.IsAbstract || type.ContainsGenericParameters ||
                    !typeof(IModule).IsAssignableFrom(type))
                    throw new InvalidOperationException("A plugin entry point must be a public, concrete IModule class.");
                var attribute = type.GetCustomAttribute<PluginAttribute>(inherit: false)
                    ?? throw new InvalidOperationException("The plugin entry point does not use the host PluginAttribute contract.");
                if (attribute.DependsOn is null || attribute.DependsOn.Any(string.IsNullOrWhiteSpace))
                    throw new InvalidOperationException("Plugin module dependency names must not be empty.");
                result.Add(new PluginDescriptor(type.FullName!, candidate.Path, type,
                    attribute.DependsOn.Distinct(StringComparer.Ordinal).ToArray(), null));
            }
            catch (Exception ex)
            {
                result.Add(Failure(candidate.Path, ex));
            }
        }
        return result;
    }

    private static void ScanDirectory(string directory, bool isolated, List<Candidate> candidates,
        List<PluginDescriptor> result)
    {
        var found = new List<Candidate>();
        try
        {
            foreach (var path in Directory.GetFiles(directory, "*.dll").Order(StringComparer.Ordinal))
            {
                try
                {
                    var entries = ReadEntries(path);
                    if (entries.Count > 1)
                    {
                        result.Add(Failure(path,
                            new InvalidOperationException("A plugin assembly must contain exactly one plugin entry point.")));
                        if (isolated) return;
                    }
                    else found.AddRange(entries);
                }
                catch (BadImageFormatException)
                {
                    // Native DLLs do not carry managed plugin metadata.
                }
                catch (Exception ex)
                {
                    result.Add(Failure(path, ex));
                    if (isolated) return;
                }
            }
        }
        catch (Exception ex)
        {
            result.Add(Failure(directory, ex));
            return;
        }

        if (isolated && found.Count > 1)
        {
            result.Add(Failure(directory,
                new InvalidOperationException("A plugin directory must contain exactly one plugin entry point.")));
            return;
        }
        candidates.AddRange(found);
    }

    private static PluginDescriptor Failure(string path, Exception error) =>
        new(Path.GetFileNameWithoutExtension(Path.TrimEndingDirectorySeparator(path)), path, null, [], error);

    private static IReadOnlyList<Candidate> ReadEntries(string path)
    {
        using var stream = File.OpenRead(path);
        using var pe = new PEReader(stream);
        if (!pe.HasMetadata) return [];
        var reader = pe.GetMetadataReader();
        var entries = new List<Candidate>();
        foreach (var handle in reader.TypeDefinitions)
        {
            var definition = reader.GetTypeDefinition(handle);
            foreach (var attributeHandle in definition.GetCustomAttributes())
            {
                var attribute = reader.GetCustomAttribute(attributeHandle);
                if (attribute.Constructor.Kind != HandleKind.MemberReference) continue;
                var constructor = reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
                if (constructor.Parent.Kind != HandleKind.TypeReference) continue;
                var attributeType = reader.GetTypeReference((TypeReferenceHandle)constructor.Parent);
                if (reader.GetString(attributeType.Namespace) != typeof(PluginAttribute).Namespace ||
                    reader.GetString(attributeType.Name) != nameof(PluginAttribute) ||
                    attributeType.ResolutionScope.Kind != HandleKind.AssemblyReference) continue;
                var source = reader.GetAssemblyReference((AssemblyReferenceHandle)attributeType.ResolutionScope);
                if (reader.GetString(source.Name) != typeof(PluginAttribute).Assembly.GetName().Name) continue;
                entries.Add(new Candidate(path, GetTypeName(reader, handle),
                    reader.GetString(reader.GetAssemblyDefinition().Name)));
                break;
            }
        }
        return entries;
    }

    private static string GetTypeName(MetadataReader reader, TypeDefinitionHandle handle)
    {
        var definition = reader.GetTypeDefinition(handle);
        var name = reader.GetString(definition.Name);
        var parent = definition.GetDeclaringType();
        if (!parent.IsNil) return $"{GetTypeName(reader, parent)}+{name}";
        var ns = reader.GetString(definition.Namespace);
        return string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
    }

    private sealed record Candidate(string Path, string Name, string AssemblyName);
}
