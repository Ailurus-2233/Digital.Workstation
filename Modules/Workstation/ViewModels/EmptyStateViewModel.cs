using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using DigitalWorkstation.Core.Abstractions.Plugins;
using Prism.Modularity;

namespace DigitalWorkstation.Workstation.ViewModels;

/// <summary>主页的模块与插件加载快照，在视图挂载时刷新。</summary>
public sealed partial class EmptyStateViewModel(IModuleCatalog moduleCatalog) : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCoreEmpty))]
    private IReadOnlyList<LoadedComponentItem> _coreModules = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCustomEmpty))]
    private IReadOnlyList<LoadedComponentItem> _customModules = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPluginsEmpty))]
    private IReadOnlyList<LoadedComponentItem> _loadedPlugins = [];

    public bool IsCoreEmpty => CoreModules.Count == 0;
    public bool IsCustomEmpty => CustomModules.Count == 0;
    public bool IsPluginsEmpty => LoadedPlugins.Count == 0;

    public void Refresh()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(assembly => !assembly.IsDynamic).ToArray();
        const string corePrefix = "DigitalWorkstation.Core.";
        CoreModules = assemblies
            .Where(assembly => assembly.GetName().Name is { } name &&
                               name.StartsWith(corePrefix, StringComparison.Ordinal) &&
                               !name.EndsWith(".resources", StringComparison.Ordinal))
            .Select(assembly => CreateItem(assembly.GetName().Name![corePrefix.Length..],
                assembly.FullName!, assembly))
            .OrderBy(item => item.Name, StringComparer.Ordinal)
            .ToArray();

        var modules = new List<LoadedComponentItem>();
        var plugins = new List<LoadedComponentItem>();
        foreach (var module in moduleCatalog.Modules.Where(module => module.State == ModuleState.Initialized))
        {
            // 使用已经加载的程序集实例，保留 Release 插件上下文，不为主页再次加载 DLL。
            var type = Type.GetType(module.ModuleType,
                name => assemblies.FirstOrDefault(assembly => assembly.FullName == name.FullName),
                typeResolver: null, throwOnError: false);
            var isPlugin = type?.IsDefined(typeof(PluginAttribute), inherit: false) == true;
            var item = CreateItem(isPlugin ? type!.Name : module.ModuleName,
                type?.FullName ?? module.ModuleName, type?.Assembly);
            (isPlugin ? plugins : modules).Add(item);
        }
        CustomModules = modules.OrderBy(item => item.Name, StringComparer.Ordinal).ToArray();
        LoadedPlugins = plugins.OrderBy(item => item.Name, StringComparer.Ordinal).ToArray();
    }

    private static LoadedComponentItem CreateItem(string name, string identity, Assembly? assembly)
    {
        var description = assembly?.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description;
        return new LoadedComponentItem(name, string.IsNullOrWhiteSpace(description) ? identity : description);
    }
}

/// <summary>加载条目的名称与悬浮说明，不参与 Shell 导航或布局状态。</summary>
public sealed record LoadedComponentItem(string Name, string Description);
