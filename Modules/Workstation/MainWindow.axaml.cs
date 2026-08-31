using Avalonia.Input;
using DigitalWorkstation.Core.Framework.Shell;
using Ursa.Controls;

namespace DigitalWorkstation.Workstation;

public partial class MainWindow : UrsaWindow
{
    public MainWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
    }

    /// <summary>
    ///     SideBar 右缘分隔条：向右拖增大宽度
    /// </summary>
    private void OnSideBarResizerDragDelta(object? sender, VectorEventArgs e)
    {
        (DataContext as MainWindowViewModel)?.ResizePanel(PanelResizeTarget.SideBar, e.Vector.X);
    }

    /// <summary>
    ///     AuxiliaryPanel 左缘分隔条：向左拖增大宽度（与指针位移反向）
    /// </summary>
    private void OnAuxiliaryPanelResizerDragDelta(object? sender, VectorEventArgs e)
    {
        (DataContext as MainWindowViewModel)?.ResizePanel(PanelResizeTarget.AuxiliaryPanel, -e.Vector.X);
    }

    /// <summary>
    ///     BottomPanel 上缘分隔条：向上拖增大高度（与指针位移反向）
    /// </summary>
    private void OnBottomPanelResizerDragDelta(object? sender, VectorEventArgs e)
    {
        (DataContext as MainWindowViewModel)?.ResizePanel(PanelResizeTarget.BottomPanel, -e.Vector.Y);
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        // 模块贡献在 Prism 模块初始化（晚于 shell 创建）时才注册，首次显示时再收集
        (DataContext as MainWindowViewModel)?.EnsureContributionsLoaded();
    }
}
