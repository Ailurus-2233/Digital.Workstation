namespace DigitalWorkstation.Core.Framework.Shell;

/// <summary>
///     SideBar 的布局状态
/// </summary>
public sealed record SideBarState
{
    public const double MinWidth = 120;
    public const double MaxWidth = 480;

    public bool Visible { get; init; }

    public double Width { get; init; } = 240;

    /// <summary>
    ///     当前内容对应的导航项 Id；收起时保留，恢复后内容不丢
    /// </summary>
    public string? ContentFor { get; init; }
}
