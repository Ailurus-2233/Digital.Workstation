namespace DigitalWorkstation.Core.Abstractions.Commands;

/// <summary>
///     声明一个命令：标注在任何类的公共实例方法上（免类级 attribute，ADR-0005），
///     经 CommandRegistration.RegisterCommands 扫描后生成 <see cref="ICommandContribution" /> 注册进容器。
///     方法签名仅支持无参 <c>void M()</c> 与 <c>Task M()</c>，非法签名在扫描时记日志跳过
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class CommandAttribute(string title) : Attribute
{
    /// <summary>
    ///     显示标题的 Language 资源键，收集时解析，缺键回退键名本身
    /// </summary>
    public string Title { get; } = title;

    /// <summary>
    ///     稳定标识：MRU 记忆与键绑定引用的依据；null = 默认「声明类全名.方法名」。
    ///     Id 冲突时后注册者丢弃并记日志
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    ///     图标的 StreamGeometry path 字符串（取 Icons 常量）；null = 无图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    ///     快捷键文本（如 "Ctrl+Shift+P"），shell 收集后生成窗口级 KeyBinding；null = 无快捷键
    /// </summary>
    public string? Gesture { get; set; }

    /// <summary>
    ///     命令列表中的排序权重，小者靠前；同 Order 按解析后的标题字典序
    /// </summary>
    public int Order { get; set; }
}
