namespace DigitalWorkstation.Core.Abstractions.Contributions;

/// <summary>
///     工具视图（ToolView，ADR-0002）的贡献元数据：由 Framework 侧 RegisterToolViews
///     扫描 <see cref="ToolViewAttribute" /> 生成并注册进容器，shell 收集后渲染到
///     <see cref="Placement" /> 对应的 Bar；激活时经容器解析 <see cref="ViewType" /> 显示内容。
///     面板收起期间其 tab 的激活操作会被 ShellLayoutState 拒绝。
/// </summary>
public sealed class ToolViewContribution
{
    /// <summary>
    ///     稳定标识，全局唯一
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    ///     显示标题，已按当前 UI 区域性解析（非资源键）
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    ///     图标的 StreamGeometry path 字符串，由 PathIcon 消费并随主题变色；null = 无图标
    /// </summary>
    public required string? IconPath { get; init; }

    /// <summary>
    ///     同一 Bar 内的默认排序权重，小者靠前
    /// </summary>
    public required int Order { get; init; }

    /// <summary>
    ///     默认栖身的 Bar；用户拖拽后的实际归属以持久化布局为准
    /// </summary>
    public required ToolViewPlacement Placement { get; init; }

    /// <summary>
    ///     是否允许用户拖拽迁移
    /// </summary>
    public required bool AllowMove { get; init; }

    /// <summary>
    ///     内容视图类型，经容器解析以支持依赖注入
    /// </summary>
    public required Type ViewType { get; init; }
}
