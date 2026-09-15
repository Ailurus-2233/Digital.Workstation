using Avalonia.Controls;
using Avalonia;
using DigitalWorkstation.Workstation.ViewModels;

namespace DigitalWorkstation.Workstation.Views;

/// <summary>
///     主页：展示产品信息和已加载的核心程序集、已初始化的 Prism 模块。
/// </summary>
public partial class EmptyStateView : UserControl
{
    public EmptyStateView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ModuleTree.SelectedItem = null;
        (DataContext as EmptyStateViewModel)?.Refresh();
    }

    private void OnModuleSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is EmptyStateViewModel model)
        {
            model.SelectedModuleName = ModuleTree.SelectedItem as string;
        }
    }
}
