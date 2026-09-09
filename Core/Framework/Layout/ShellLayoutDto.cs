using DigitalWorkstation.Core.Abstractions.Contributions;

namespace DigitalWorkstation.Core.Framework.Layout;

/// <summary>
///     布局持久化 DTO（ADR-0002）：layout.json 落盘的专用格式，独立于
///     <see cref="ShellLayoutState" />（状态机只管流转语义，不管序列化兼容）。
///     <see cref="Version" /> 不识别的文件由 LayoutPersistence 整体丢弃，调用方静默默认布局
/// </summary>
public sealed record ShellLayoutDto
{
    /// <summary>
    ///     当前布局格式版本
    /// </summary>
    public const int CurrentVersion = 1;

    public int Version { get; init; } = CurrentVersion;

    /// <summary>
    ///     面板对齐档位（不在 ShellLayoutState 里，由 FrameworkWindow 依赖属性持有，一并持久化）
    /// </summary>
    public PanelAlignment PanelAlignment { get; init; } = PanelAlignment.Center;

    /// <summary>
    ///     可移动工具视图 Id → 归属 Bar 与 Bar 内序号；钉住项（AllowMove=false）恒在
    ///     ActivityBar 底部段，不入此表。恢复时优先于 attribute 的 Default；
    ///     无对应贡献的孤儿条目丢弃，无条目的新工具视图落回 Default
    /// </summary>
    public Dictionary<string, ToolViewPlacementEntry> Placements { get; init; } = [];

    public SideBarLayoutDto? SideBar { get; init; }

    public PanelLayoutDto? AuxiliaryPanel { get; init; }

    public BottomPanelLayoutDto? BottomPanel { get; init; }
}

/// <summary>
///     一个工具视图的持久化归属
/// </summary>
public sealed record ToolViewPlacementEntry
{
    public ToolViewPlacement Bar { get; init; }

    /// <summary>
    ///     Bar 内序号，小者靠前
    /// </summary>
    public int Index { get; init; }
}

/// <summary>
///     SideBar 的持久化布局
/// </summary>
public sealed record SideBarLayoutDto
{
    public bool Visible { get; init; }

    public double Width { get; init; } = 240;

    /// <summary>
    ///     选中的导航项 Id；收起时也保留（与 SideBarState.ContentFor 同语义）
    /// </summary>
    public string? Selected { get; init; }
}

/// <summary>
///     AuxiliaryPanel 的持久化布局
/// </summary>
public sealed record PanelLayoutDto
{
    public bool Visible { get; init; } = true;

    public double Width { get; init; } = 280;

    public string? ActiveTab { get; init; }
}

/// <summary>
///     BottomPanel 的持久化布局
/// </summary>
public sealed record BottomPanelLayoutDto
{
    public bool Visible { get; init; } = true;

    public double Height { get; init; } = 160;

    public string? ActiveTab { get; init; }
}
