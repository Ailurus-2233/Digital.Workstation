using Avalonia.Controls;
using Avalonia;
using Prism.Modularity;

namespace DigitalWorkstation.Workstation.Views;

/// <summary>
///     主页：展示产品信息和已加载的核心程序集、已初始化的 Prism 模块。
/// </summary>
public partial class EmptyStateView : UserControl
{
    private readonly IModuleCatalog _moduleCatalog;

    public EmptyStateView(IModuleCatalog moduleCatalog)
    {
        _moduleCatalog = moduleCatalog;
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        // 主页在启动序列结束后挂入可视树；回到主页时重新读取，避免缓存启动前的空目录。
        const string corePrefix = "DigitalWorkstation.Core.";
        var core = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic)
            .Select(assembly => assembly.GetName().Name)
            .OfType<string>()
            .Where(name => name.StartsWith(corePrefix, StringComparison.Ordinal)
                           && !name.EndsWith(".resources", StringComparison.Ordinal))
            .Select(name => name[corePrefix.Length..])
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        var custom = _moduleCatalog.Modules
            .Where(module => module.State == ModuleState.Initialized)
            .Select(module => module.ModuleName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        CoreList.ItemsSource = core;
        CustomList.ItemsSource = custom;
        CoreEmpty.IsVisible = core.Length == 0;
        CustomEmpty.IsVisible = custom.Length == 0;
    }
}
