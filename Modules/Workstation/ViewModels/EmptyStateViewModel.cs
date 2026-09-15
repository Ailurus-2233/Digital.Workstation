using CommunityToolkit.Mvvm.ComponentModel;
using Prism.Modularity;

namespace DigitalWorkstation.Workstation.ViewModels;

/// <summary>主页的加载快照与模块选择；在视图挂载时刷新，不在构造期读取模块状态。</summary>
public sealed partial class EmptyStateViewModel(IModuleCatalog moduleCatalog) : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCoreEmpty))]
    private IReadOnlyList<string> _coreModules = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCustomEmpty))]
    private IReadOnlyList<string> _customModules = [];

    [ObservableProperty]
    private string? _selectedModuleName;

    public bool IsCoreEmpty => CoreModules.Count == 0;
    public bool IsCustomEmpty => CustomModules.Count == 0;

    public void Refresh()
    {
        SelectedModuleName = null;
        const string corePrefix = "DigitalWorkstation.Core.";
        CoreModules = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic)
            .Select(assembly => assembly.GetName().Name)
            .OfType<string>()
            .Where(name => name.StartsWith(corePrefix, StringComparison.Ordinal)
                           && !name.EndsWith(".resources", StringComparison.Ordinal))
            .Select(name => name[corePrefix.Length..])
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        CustomModules = moduleCatalog.Modules
            .Where(module => module.State == ModuleState.Initialized)
            .Select(module => module.ModuleName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
    }
}
