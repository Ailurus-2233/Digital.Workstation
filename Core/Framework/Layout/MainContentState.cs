namespace DigitalWorkstation.Core.Framework.Layout;

/// <summary>
///     当前主标签对应的视图贡献，主页时为空。文档身份由标签持有。
/// </summary>
public sealed record MainContentState
{
    public string? ActiveView { get; init; }
}
