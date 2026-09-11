namespace DigitalWorkstation.Core.Abstractions.Settings;

/// <summary>
///     设置分组的贡献元数据：由 Framework 侧 RegisterSettings 扫描 <see cref="SettingGroupAttribute" />
///     生成并注册进容器；收集时按 <see cref="Name" /> 全局合并（多处声明取最小 <see cref="Order" />），
///     仅被设置项引用而无声明的分组由收集侧补出（Order 视为 0）
/// </summary>
public sealed class SettingGroupContribution
{
    /// <summary>
    ///     分组显示名的 Language 资源键（非已解析文案），运行时经 Language.Get 解析，缺键回退键名本身
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    ///     分组在设置页分组树中的排序权重，小者靠前；同名多处声明取最小值
    /// </summary>
    public required int Order { get; init; }
}
