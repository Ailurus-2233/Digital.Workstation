using System.Windows.Input;

namespace DigitalWorkstation.Core.Abstractions.Shell;

/// <summary>
///     模块向快速工具栏追加按钮的契约。
///     模块在 <see cref="Prism.Ioc.IContainerRegistry" /> 中以本接口注册实现，
///     shell 收集后按 <see cref="Order" /> 渲染为图标按钮，<see cref="Title" /> 作为 ToolTip。
/// </summary>
public interface IToolBarItemContribution
{
    /// <summary>
    ///     工具栏项的稳定标识，全局唯一
    /// </summary>
    string Id { get; }

    /// <summary>
    ///     显示标题（ToolTip）
    /// </summary>
    string Title { get; }

    /// <summary>
    ///     图标的 StreamGeometry path 字符串，由 PathIcon 消费并随主题变色
    /// </summary>
    string IconPath { get; }

    /// <summary>
    ///     排序权重，小者靠前
    /// </summary>
    int Order { get; }

    /// <summary>
    ///     点击按钮执行的命令
    /// </summary>
    ICommand Command { get; }
}
