namespace DigitalWorkstation.Core.Abstractions.Settings;

/// <summary>
///     设置分组的贡献元数据：由 Framework 侧 RegisterSettings 扫描 <see cref="SettingGroupAttribute" />
///     生成并注册进容器；收集时按 <see cref="Id" /> 全局合并（首个声明的资源来源与名称生效，Order 取最小），
///     仅被设置项引用而无声明的分组由收集侧补出（Name 为 Id，ResourceType 为 null，Order 为 0）
/// </summary>
public sealed class SettingGroupContribution
{
    /// <summary>
    ///     稳定分组标识，设置项通过 Group 引用
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    ///     分组显示名的资源所属类型；仅无显式声明的隐式分组为 null
    /// </summary>
    public required Type? ResourceType { get; init; }

    /// <summary>
    ///     分组显示名的资源键（非已解析文案）；隐式分组直接使用 Id，不做资源查找
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    ///     分组在设置页分组树中的排序权重，小者靠前；同 Id 多处声明取最小值
    /// </summary>
    public required int Order { get; init; }
}
