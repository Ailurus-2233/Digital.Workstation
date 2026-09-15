namespace DigitalWorkstation.Core.Abstractions.Settings;

/// <summary>
///     声明一个设置分组：标注在任何类上，经 SettingRegistration.RegisterSettings 扫描注册（ADR-0006 (https://github.com/Ailurus-2233/Digital.Workstation/blob/main/docs/adr/0006-attribute-settings-registration.md) 决策 1）。
///     分组按稳定 Id 全局合并——同 Id 首个声明的资源来源与名称生效，位次取最小；
///     设置项经 <see cref="SettingItemAttribute.Group" /> 引用分组 Id 归组，
///     没有任何本 attribute 声明的分组也可用（由设置项引用隐式产生，位次视为 0）
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class SettingGroupAttribute(string id, Type resourceType, string name) : Attribute
{
    /// <summary>
    ///     稳定分组标识，与显示名称及资源键无关
    /// </summary>
    public string Id { get; } = id;

    /// <summary>
    ///     分组显示名的资源所属类型
    /// </summary>
    public Type ResourceType { get; } = resourceType;

    /// <summary>
    ///     分组显示名的资源键，设置页构造时从 ResourceType 解析，缺键回退键名本身
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    ///     分组在设置页分组树中的排序权重，小者靠前；同 Id 多处声明冲突时取最小值
    /// </summary>
    public int Order { get; set; }
}
