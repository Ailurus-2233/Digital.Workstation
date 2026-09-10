using Avalonia.Input;

namespace DigitalWorkstation.Core.Framework.Layout;

/// <summary>
///     工具视图拖拽会话（ADR-0002）：<see cref="ToolViewButton" /> 在 DoDragDrop 期间把
///     <see cref="IsActive" /> 置为 true，shell 借此临时显露隐藏面板的投放区——
///     「向隐藏面板拖入则自动显示」的前提是拖拽进行中隐藏面板存在落点
/// </summary>
public static class ToolViewDragSession
{
    /// <summary>
    ///     拖拽负载的数据格式；负载为工具视图 Id 字符串（仅应用内拖拽，不走系统格式）
    /// </summary>
    public static readonly DataFormat<string> TabIdFormat =
        DataFormat.CreateStringApplicationFormat("DigitalWorkstation.ToolView");

    public static bool IsActive { get; private set; }

    /// <summary>
    ///     IsActive 变化通知；拖拽手势在 UI 线程发起与结束，回调同在 UI 线程
    /// </summary>
    public static event Action<bool>? ActiveChanged;

    public static void Begin()
    {
        IsActive = true;
        ActiveChanged?.Invoke(true);
    }

    public static void End()
    {
        IsActive = false;
        ActiveChanged?.Invoke(false);
    }
}
