using Avalonia.Controls;

namespace DigitalWorkstation.Workstation;

/// <summary>
///     面板分隔条：复用 GridSplitter 的拖拽手势与方向光标，但禁用其原生列重排——
///     面板尺寸的唯一来源是 ShellLayoutState，拖动增量经 DragDelta 事件交给 ViewModel 的状态转换（含 clamp）
/// </summary>
public class PanelResizer : GridSplitter
{
    /// <summary>
    ///     ControlTheme 按 StyleKey 精确查找：继承 GridSplitter 的主题（模板/尺寸/焦点行为）
    /// </summary>
    protected override Type StyleKeyOverride => typeof(GridSplitter);

    /// <summary>
    ///     返回 null 使原生 resize 初始化短路：ResizeData 为空，GridSplitter 的所有原生重排路径自动跳过，
    ///     只剩 Thumb 的 DragStarted/DragDelta/DragCompleted 事件
    /// </summary>
    protected override Grid? GetParentGrid()
    {
        return null;
    }
}
