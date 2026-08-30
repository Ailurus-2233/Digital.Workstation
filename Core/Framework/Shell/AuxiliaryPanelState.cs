namespace DigitalWorkstation.Core.Framework.Shell;

/// <summary>
///     AuxiliaryPanel 的布局状态
/// </summary>
public sealed record AuxiliaryPanelState
{
    public const double MinWidth = 120;
    public const double MaxWidth = 480;

    public bool Visible { get; init; } = true;

    public double Width { get; init; } = 280;

    public IReadOnlyList<string> Tabs { get; init; } = [];

    /// <summary>
    ///     收起时保留；面板收起期间激活 tab 的操作会被拒绝
    /// </summary>
    public string? ActiveTab { get; init; }
}
