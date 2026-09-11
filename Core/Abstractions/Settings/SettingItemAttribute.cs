namespace DigitalWorkstation.Core.Abstractions.Settings;

/// <summary>
///     声明一个设置项：标注在公共静态可读属性上，经 SettingRegistration.RegisterSettings
///     扫描后生成 <see cref="SettingItemContribution" /> 注册进容器（ADR-0006 决策 1）。
///     属性只是声明锚点——属性类型即设置值类型，扫描不读取属性值，读写一律经 ISettingsService；
///     非公共/非静态/无 getter 的属性连候选都进不了（静默忽略，同菜单/命令扫描惯例）
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SettingItemAttribute(string group, string name) : Attribute
{
    /// <summary>
    ///     稳定标识；null = 默认「声明类全名.属性名」（仿命令 Id 规则，ADR-0005）。
    ///     全局唯一，是 settings.json 的 key 与 ISettingsService 读写的依据
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    ///     所属分组的名称键（<see cref="SettingGroupAttribute.Name" />），按名称全局合并归组
    /// </summary>
    public string Group { get; } = group;

    /// <summary>
    ///     设置项显示名的 Language 资源键，运行时解析，缺键回退键名本身（ADR-0006 决策 2）
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    ///     默认值：用户从未修改时 <see cref="ISettingsService.Get{T}" /> 的返回值（ADR-0006 决策 3）。
    ///     必须是属性类型的编译期常量（attribute 实参限制）；类型不匹配在扫描时记日志跳过。
    ///     枚举成员显示名走 Language 资源键，键按「设置项名称键 + 成员名」约定生成，缺键回退成员名本身（决策 8）
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    ///     同分组内的排序权重，小者靠前；同 Order 按名称键字典序
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    ///     是否需重启生效：修改后值立即落盘、当前进程行为不变、下次启动由消费方读取生效（ADR-0006 决策 7）
    /// </summary>
    public bool RequiresRestart { get; set; }
}
