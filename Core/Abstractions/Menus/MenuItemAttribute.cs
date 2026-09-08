namespace DigitalWorkstation.Core.Abstractions.Menus;

/// <summary>
///     声明一个菜单项：标注在菜单类（<see cref="MenuGroupAttribute" />）的公共实例方法上。
///     方法签名仅支持无参 <c>void M()</c> 与 <c>Task M()</c>，非法签名在扫描时记日志跳过。
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class MenuItemAttribute(string title) : Attribute
{
    /// <summary>
    ///     显示标题的 Language 资源键，运行时解析，缺键回退键名本身
    /// </summary>
    public string Title { get; } = title;

    /// <summary>
    ///     同组内的排序权重，小者靠前；同 Order 按解析后的标题字典序
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    ///     图标的 StreamGeometry path 字符串（取 Icons 常量）；null = 无图标
    /// </summary>
    public string? Icon { get; set; }
}
