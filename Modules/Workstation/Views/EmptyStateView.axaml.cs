using Avalonia.Controls;
using Avalonia;
using DigitalWorkstation.Workstation.ViewModels;

namespace DigitalWorkstation.Workstation.Views;

/// <summary>
///     主页：展示产品信息和已加载的核心程序集、已初始化的内置模块与插件；说明通过条目悬浮提示呈现。
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
}
