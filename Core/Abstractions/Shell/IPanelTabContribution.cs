namespace DigitalWorkstation.Core.Abstractions.Shell;

/// <summary>
///     面板 tab 所属的面板
/// </summary>
public enum PanelPlacement
{
    /// <summary>
    ///     右侧 AuxiliaryPanel
    /// </summary>
    Auxiliary,

    /// <summary>
    ///     底部 BottomPanel
    /// </summary>
    Bottom
}

/// <summary>
///     模块向 AuxiliaryPanel / BottomPanel 贡献面板 tab 的契约。
///     模块在 <see cref="Prism.Ioc.IContainerRegistry" /> 中以本接口注册实现，
///     shell 收集后按 <see cref="Panel" /> 与 <see cref="Order" /> 渲染到对应面板的 tab 栏；
///     tab 激活时面板内容区显示 <see cref="ContentViewType" /> 解析出的视图。
///     面板收起期间其 tab 的激活操作会被 ShellLayoutState 拒绝。
/// </summary>
public interface IPanelTabContribution
{
    /// <summary>
    ///     tab 的稳定标识，全局唯一（跨两个面板）
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
    ///     同一面板内的排序权重，小者靠前
    /// </summary>
    int Order { get; }

    /// <summary>
    ///     贡献到右侧 AuxiliaryPanel 还是底部 BottomPanel
    /// </summary>
    PanelPlacement Panel { get; }

    /// <summary>
    ///     激活该 tab 时面板内容区显示的视图类型，经容器解析以支持依赖注入
    /// </summary>
    Type ContentViewType { get; }
}
