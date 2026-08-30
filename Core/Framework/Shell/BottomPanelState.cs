namespace DigitalWorkstation.Core.Framework.Shell;

/// <summary>
///     BottomPanel 的布局状态
/// </summary>
public sealed record BottomPanelState
{
    public const double MinHeight = 80;
    public const double MaxHeight = 480;

    public bool Visible { get; init; } = true;

    public double Height { get; init; } = 160;

    public IReadOnlyList<string> Tabs { get; init; } = [];

    /// <summary>
    ///     收起时保留；面板收起期间激活 tab 的操作会被拒绝
    /// </summary>
    public string? ActiveTab { get; init; }
}
