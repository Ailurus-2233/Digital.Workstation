namespace DigitalWorkstation.Core.Abstractions.Settings;

/// <summary>
///     设置项的贡献元数据：由 Framework 侧 RegisterSettings 扫描 <see cref="SettingItemAttribute" />
///     生成并注册进容器。设置页据此渲染编辑器（控件由 <see cref="ValueType" /> 推断，ADR-0006 决策 8），
///     ISettingsService 据此取 <see cref="DefaultValue" /> 作为未修改时的读值
/// </summary>
public sealed class SettingItemContribution
{
    /// <summary>
    ///     稳定标识，全局唯一：默认「声明类全名.属性名」；settings.json 的 key 与 ISettingsService 读写的依据
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    ///     所属分组的名称键（SettingGroupAttribute.Name），按名称全局合并归组
    /// </summary>
    public required string Group { get; init; }

    /// <summary>
    ///     设置项显示名的 Language 资源键（非已解析文案），缺键回退键名本身
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    ///     设置值类型（声明属性的类型）：编辑器推断与 JSON 反序列化的依据
    /// </summary>
    public required Type ValueType { get; init; }

    /// <summary>
    ///     默认值：用户从未修改时的取值，不是单独存储层（ADR-0006 决策 3）
    /// </summary>
    public required object? DefaultValue { get; init; }

    /// <summary>
    ///     同分组内的排序权重，小者靠前；同 Order 按 <see cref="Name" /> 键字典序
    /// </summary>
    public required int Order { get; init; }

    /// <summary>
    ///     是否需重启生效：修改后值立即落盘、当前进程行为不变、下次启动生效（ADR-0006 决策 7）
    /// </summary>
    public required bool RequiresRestart { get; init; }
}
