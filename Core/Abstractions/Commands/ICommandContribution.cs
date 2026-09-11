using System.Windows.Input;

namespace DigitalWorkstation.Core.Abstractions.Commands;

/// <summary>
///     模块向全局命令列表贡献命令的契约（ADR-0005）。
///     通常不直接实现本接口：模块用 <see cref="CommandAttribute" /> 标注普通类的方法，
///     经 CommandRegistration.RegisterCommands 扫描后生成本契约的实现注册进容器；
///     shell 收集全部实现后交给命令面板呈现，并为带 <see cref="Gesture" /> 的命令生成窗口级 KeyBinding
/// </summary>
public interface ICommandContribution
{
    /// <summary>
    ///     稳定标识：默认「声明类全名.方法名」，MRU 记忆与键绑定引用的依据；全局唯一，冲突者被丢弃
    /// </summary>
    string Id { get; }

    /// <summary>
    ///     显示标题（已按当前 UI 区域性解析）
    /// </summary>
    string Title { get; }

    /// <summary>
    ///     快捷键文本（如 "Ctrl+Shift+P"）；null = 无快捷键
    /// </summary>
    string? Gesture { get; }

    /// <summary>
    ///     图标的 StreamGeometry path 字符串，由 PathIcon 消费并随主题变色；null = 无图标
    /// </summary>
    string? IconPath { get; }

    /// <summary>
    ///     命令列表中的排序权重，小者靠前；同 Order 按解析后的 <see cref="Title" /> 字典序
    /// </summary>
    int Order { get; }

    /// <summary>
    ///     执行命令（命令面板选中或快捷键触发时调用）
    /// </summary>
    ICommand Command { get; }
}
