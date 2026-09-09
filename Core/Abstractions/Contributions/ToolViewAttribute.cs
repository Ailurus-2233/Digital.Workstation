namespace DigitalWorkstation.Core.Abstractions.Contributions;

/// <summary>
///     工具视图默认栖身的 Bar
/// </summary>
public enum ToolViewPlacement
{
    /// <summary>
    ///     ActivityBar（内容显示在 SideBar）
    /// </summary>
    ActivityBar,

    /// <summary>
    ///     右侧 AuxiliaryPanel
    /// </summary>
    AuxiliaryPanel,

    /// <summary>
    ///     底部 BottomPanel
    /// </summary>
    BottomPanel
}

/// <summary>
///     声明一个 View 类是工具视图（ToolView，ADR-0002）：带图标与标题的可停靠界面单元。
///     由模块 RegisterTypes 中的 RegisterToolViews(Assembly) 扫描注册；
///     <see cref="Default" /> 只是默认归属——用户拖拽后的实际归属以持久化布局为准。
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ToolViewAttribute(string id, string titleKey) : Attribute
{
    /// <summary>
    ///     稳定标识，全局唯一，约定模块名前缀（如 "shell.outline"）
    /// </summary>
    public string Id { get; } = id;

    /// <summary>
    ///     显示标题的 Language 资源键，注册时解析，缺键回退键名本身
    /// </summary>
    public string TitleKey { get; } = titleKey;

    /// <summary>
    ///     图标的 StreamGeometry path 字符串（取 Icons 常量）；null = 无图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    ///     默认栖身的 Bar
    /// </summary>
    public ToolViewPlacement Default { get; set; } = ToolViewPlacement.AuxiliaryPanel;

    /// <summary>
    ///     同一 Bar 内的默认排序权重，小者靠前
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    ///     是否允许用户拖拽迁移；false 且 <see cref="Default" /> 为 ActivityBar 时钉在 ActivityBar 底部段
    /// </summary>
    public bool AllowMove { get; set; } = true;
}
