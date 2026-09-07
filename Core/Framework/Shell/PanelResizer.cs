using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace DigitalWorkstation.Core.Framework.Shell;

/// <summary>
///     面板分隔条：复用 GridSplitter 的拖拽手势与方向光标，但禁用其原生列重排——
///     面板尺寸的唯一来源是 ShellLayoutState。拖拽增量按 Target 换算方向（含取反）后执行 ResizeCommand，
///     由 ViewModel 做状态转换（含 clamp）
/// </summary>
public class PanelResizer : GridSplitter
{
    public static readonly StyledProperty<ICommand?> ResizeCommandProperty =
        AvaloniaProperty.Register<PanelResizer, ICommand?>(nameof(ResizeCommand));

    public PanelResizer()
    {
        DragDelta += OnDragDelta;
    }

    /// <summary>
    ///     ControlTheme 按 StyleKey 精确查找：继承 GridSplitter 的主题（模板/尺寸/焦点行为）
    /// </summary>
    protected override Type StyleKeyOverride => typeof(GridSplitter);

    /// <summary>
    ///     拖拽调整的目标区域：决定尺寸增量取哪个轴、是否取反
    /// </summary>
    public PanelResizeTarget Target { get; set; }

    /// <summary>
    ///     拖拽增量的出口：ViewModel 的 ResizePanelCommand
    /// </summary>
    public ICommand? ResizeCommand
    {
        get => GetValue(ResizeCommandProperty);
        set => SetValue(ResizeCommandProperty, value);
    }

    /// <summary>
    ///     返回 null 使原生 resize 初始化短路：ResizeData 为空，GridSplitter 的所有原生重排路径自动跳过，
    ///     只剩 Thumb 的 DragStarted/DragDelta/DragCompleted 事件
    /// </summary>
    protected override Grid? GetParentGrid()
    {
        return null;
    }

    /// <summary>
    ///     方向换算：SideBar 向右拖增大宽度；AuxiliaryPanel 向左拖增大宽度；BottomPanel 向上拖增大高度
    /// </summary>
    private void OnDragDelta(object? sender, VectorEventArgs e)
    {
        var delta = Target switch
        {
            PanelResizeTarget.SideBar => e.Vector.X,
            PanelResizeTarget.AuxiliaryPanel => -e.Vector.X,
            _ => -e.Vector.Y
        };
        ResizeCommand?.Execute(new PanelResize(Target, delta));
    }
}
