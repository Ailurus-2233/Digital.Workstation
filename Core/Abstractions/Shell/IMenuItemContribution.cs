using System.Windows.Input;

namespace DigitalWorkstation.Core.Abstractions.Shell;

/// <summary>
///     菜单项所属的顶层菜单
/// </summary>
public enum MenuPlacement
{
    /// <summary>
    ///     文件菜单
    /// </summary>
    File,

    /// <summary>
    ///     视图菜单
    /// </summary>
    View,

    /// <summary>
    ///     帮助菜单
    /// </summary>
    Help
}

/// <summary>
///     模块向菜单栏追加菜单项的契约。
///     模块在 <see cref="Prism.Ioc.IContainerRegistry" /> 中以本接口注册实现，
///     shell 收集后按 <see cref="Menu" /> 与 <see cref="Order" /> 渲染到对应顶层菜单；
///     shell 预置项（退出、面板显隐切换、关于）与模块贡献项经同一机制渲染。
/// </summary>
public interface IMenuItemContribution
{
    /// <summary>
    ///     菜单项的稳定标识，全局唯一
    /// </summary>
    string Id { get; }

    /// <summary>
    ///     显示标题
    /// </summary>
    string Title { get; }

    /// <summary>
    ///     图标的 StreamGeometry path 字符串，由 PathIcon 消费并随主题变色
    /// </summary>
    string IconPath { get; }

    /// <summary>
    ///     同一菜单内的排序权重，小者靠前
    /// </summary>
    int Order { get; }

    /// <summary>
    ///     追加到哪个顶层菜单
    /// </summary>
    MenuPlacement Menu { get; }

    /// <summary>
    ///     点击菜单项执行的命令
    /// </summary>
    ICommand Command { get; }
}
