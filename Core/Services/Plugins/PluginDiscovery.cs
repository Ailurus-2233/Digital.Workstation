using System.Globalization;
using System.Resources;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.Loader;
using DigitalWorkstation.Core.Abstractions.Plugins;

namespace DigitalWorkstation.Core.Services.Plugins;

/// <summary>读取 PE 元数据后筛选入口，禁用插件不加载程序集或执行代码。</summary>
internal static class PluginDiscovery
{
    public static IReadOnlyList<PluginDescriptor> Discover(IReadOnlyCollection<Assembly> sharedModules,
        PluginStartupService startup)
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
        catch (Exception ex) { result.Add(Failure(root, ex)); }
#endif
        foreach (var candidate in candidates) startup.MigrateIdentity(candidate.Name, candidate.Key);
        var enabled = candidates.Where(c => startup.IsEnabledAtStartup(c.Key)).ToArray();
        var duplicateKeys = enabled.GroupBy(c => c.Key, StringComparer.Ordinal)
            .Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet(StringComparer.Ordinal);
        var duplicateAssemblies = enabled.GroupBy(c => c.AssemblyName, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in candidates)
        {
            var descriptor = new PluginDescriptor(candidate.Name, candidate.Path, null, [], null)
                { Key = candidate.Key, DisplayName = candidate.DisplayName };
            if (!startup.IsEnabledAtStartup(candidate.Key)) { result.Add(descriptor); continue; }
            if (duplicateKeys.Contains(candidate.Key) || duplicateAssemblies.Contains(candidate.AssemblyName))
            {
                result.Add(descriptor with { Error = new InvalidOperationException(
                    $"Duplicate plugin key or assembly identity: {candidate.Key} ({candidate.AssemblyName}).") });
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
                result.Add(descriptor with { ModuleType = type,
                    Dependencies = attribute.DependsOn.Distinct(StringComparer.Ordinal).ToArray() });
            }
            catch (Exception ex) { result.Add(descriptor with { Error = ex }); }
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
                        result.Add(Failure(path, new InvalidOperationException(
                            "A plugin assembly must contain exactly one plugin entry point.")));
                        if (isolated) return;
                    }
                    else found.AddRange(entries);
                }
                catch (BadImageFormatException) { }
                catch (Exception ex) { result.Add(Failure(path, ex)); if (isolated) return; }
            }
        }
        catch (Exception ex) { result.Add(Failure(directory, ex)); return; }
        if (isolated && found.Count > 1)
        {
            result.Add(Failure(directory, new InvalidOperationException(
                "A plugin directory must contain exactly one plugin entry point.")));
            return;
        }
        candidates.AddRange(found);
    }

    private static PluginDescriptor Failure(string path, Exception error) =>
        new(Path.GetFileNameWithoutExtension(Path.TrimEndingDirectorySeparator(path)), path, null, [], error);

    internal static bool IsPluginAssembly(string path) => ReadEntries(path).Count == 1;

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
                var entryName = GetTypeName(reader, handle);
                var signature = reader.GetBlobReader(constructor.Signature);
                signature.ReadSignatureHeader();
                var parameterCount = signature.ReadCompressedInteger();
                var blob = reader.GetBlobReader(attribute.Value);
                if (blob.ReadUInt16() != 1) throw new InvalidOperationException("Invalid plugin attribute.");
                var displayName = entryName;
                var key = entryName;
                if (parameterCount == 3)
                {
                    key = blob.ReadSerializedString()!;
                    var owner = blob.ReadSerializedString();
                    var nameKey = blob.ReadSerializedString();
                    if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(owner) ||
                        string.IsNullOrWhiteSpace(nameKey))
                        throw new InvalidOperationException("Plugin key and name resource must not be empty.");
                    var parts = owner.Split(',', 2);
                    var resourceTypeName = parts[0].Trim();
                    // 同程序集 typeof 参数由编译器序列化为未限定类型名。
                    var assemblyMatches = parts.Length == 1 ||
                        new AssemblyName(parts[1].Trim()).Name ==
                        reader.GetString(reader.GetAssemblyDefinition().Name);
                    var typeExists = reader.TypeDefinitions.Any(typeHandle =>
                        GetTypeName(reader, typeHandle) == resourceTypeName);
                    if (!assemblyMatches || !typeExists)
                        throw new InvalidOperationException("Plugin name resources must belong to the plugin assembly.");
                    displayName = ReadDisplayName(path, resourceTypeName, nameKey);
                }
                entries.Add(new Candidate(path, entryName,
                    reader.GetString(reader.GetAssemblyDefinition().Name), displayName, key));
                break;
            }
        }
        return entries;
    }

    // 直接读取资源数据，禁用插件也能显示本地化名称，无需加载程序集或运行插件代码。
    private static string ReadDisplayName(string assemblyPath, string owner, string key)
    {
        var assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
        for (var culture = CultureInfo.CurrentUICulture; !string.IsNullOrEmpty(culture.Name); culture = culture.Parent)
        {
            var satellite = Path.Combine(Path.GetDirectoryName(assemblyPath)!, culture.Name, assemblyName + ".resources.dll");
            if (!File.Exists(satellite)) continue;
            var value = ReadResource(satellite, owner + "." + culture.Name + ".resources", key);
            if (value is not null) return value;
        }
        return ReadResource(assemblyPath, owner + ".resources", key) ?? key;
    }

    private static string? ReadResource(string assemblyPath, string resourceName, string key)
    {
        using var stream = File.OpenRead(assemblyPath);
        using var pe = new PEReader(stream);
        var metadata = pe.GetMetadataReader();
        foreach (var handle in metadata.ManifestResources)
        {
            var resource = metadata.GetManifestResource(handle);
            if (!resource.Implementation.IsNil || metadata.GetString(resource.Name) != resourceName) continue;
            var directory = pe.PEHeaders.CorHeader!.ResourcesDirectory;
            var block = pe.GetSectionData(directory.RelativeVirtualAddress).GetReader((int)resource.Offset,
                directory.Size - (int)resource.Offset);
            var length = block.ReadInt32();
            using var data = new MemoryStream(block.ReadBytes(length));
            using var resources = new ResourceReader(data);
            var entries = resources.GetEnumerator();
            while (entries.MoveNext())
                if (entries.Key is string entryKey && entryKey == key)
                    return entries.Value as string;
            return null;
        }
        return null;
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

    private sealed record Candidate(string Path, string Name, string AssemblyName, string DisplayName, string Key);
}