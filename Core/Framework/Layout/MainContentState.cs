namespace DigitalWorkstation.Core.Framework.Layout;

/// <summary>
///     MainContent 的布局状态，单视图切换
/// </summary>
public sealed record MainContentState
{
    public string? ActiveView { get; init; }
}
