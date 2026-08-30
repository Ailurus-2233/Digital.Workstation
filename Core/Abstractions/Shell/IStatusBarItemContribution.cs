namespace DigitalWorkstation.Core.Abstractions.Shell;

/// <summary>
///     模块向状态栏追加条目的契约。
///     模块在 <see cref="Prism.Ioc.IContainerRegistry" /> 中以本接口注册实现，
///     shell 收集后按 <see cref="Order" /> 渲染为"图标 + 文本"的状态指示。
/// </summary>
public interface IStatusBarItemContribution
{
    /// <summary>
    ///     状态栏项的稳定标识，全局唯一
    /// </summary>
    string Id { get; }

    /// <summary>
    ///     显示文本
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
}
