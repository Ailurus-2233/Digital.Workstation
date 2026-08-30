using Avalonia.Controls;

namespace DigitalWorkstation.Workstation.Views;

/// <summary>
///     Shell 内置空状态页：MainContent 尚无活动视图时显示的静态快捷键提示，
///     不依赖任何模块贡献
/// </summary>
public partial class EmptyStateView : UserControl
{
    public EmptyStateView()
    {
        InitializeComponent();
    }
}
