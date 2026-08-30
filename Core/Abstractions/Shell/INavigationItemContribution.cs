namespace DigitalWorkstation.Core.Abstractions.Shell;

/// <summary>
///     导航项在 ActivityBar 中的放置位置
/// </summary>
public enum NavigationItemPlacement
{
    /// <summary>
    ///     ActivityBar 顶部，模块贡献的功能域导航
    /// </summary>
    Top,

    /// <summary>
    ///     ActivityBar 底部，设置类入口
    /// </summary>
    Bottom
}

/// <summary>
///     模块向 ActivityBar 贡献导航项的契约。
///     模块在 <see cref="Prism.Ioc.IContainerRegistry" /> 中以本接口注册实现，
///     shell 收集后按 <see cref="Placement" /> 与 <see cref="Order" /> 渲染；
///     点击导航项时 SideBar 显示 <see cref="ContentViewType" /> 解析出的视图。
/// </summary>
public interface INavigationItemContribution
{
    /// <summary>
    ///     导航项的稳定标识，同一模块内唯一
    /// </summary>
    string Id { get; }

    /// <summary>
    ///     显示标题（ToolTip 与 SideBar 标题）
    /// </summary>
    string Title { get; }

    /// <summary>
    ///     图标的 StreamGeometry path 字符串，由 PathIcon 消费并随主题变色
    /// </summary>
    string IconPath { get; }

    /// <summary>
    ///     同一 <see cref="Placement" /> 内的排序权重，小者靠前
    /// </summary>
    int Order { get; }

    /// <summary>
    ///     放置在 ActivityBar 顶部还是底部
    /// </summary>
    NavigationItemPlacement Placement { get; }

    /// <summary>
    ///     选中该导航项时 SideBar 显示的内容视图类型，经容器解析以支持依赖注入
    /// </summary>
    Type ContentViewType { get; }
}
