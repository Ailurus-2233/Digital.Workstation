namespace DigitalWorkstation.Core.Abstractions.Settings;

/// <summary>
///     声明一个设置分组：标注在任何类上，经 SettingRegistration.RegisterSettings 扫描注册（ADR-0006 决策 1）。
///     分组按名称全局合并——不同模块声明的同名分组是同一个分组；
///     设置项经 <see cref="SettingItemAttribute.Group" /> 引用分组名称键归组，
///     没有任何本 attribute 声明的分组也可用（由设置项引用隐式产生，位次视为 0）
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class SettingGroupAttribute(string name) : Attribute
{
    /// <summary>
    ///     分组显示名的 Language 资源键，运行时解析，缺键回退键名本身
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    ///     分组在设置页分组树中的排序权重，小者靠前；同名分组多处声明冲突时取最小值（同 ADR-0001 决策 4）
    /// </summary>
    public int Order { get; set; }
}
