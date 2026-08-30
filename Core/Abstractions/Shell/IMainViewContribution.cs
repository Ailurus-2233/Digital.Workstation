namespace DigitalWorkstation.Core.Abstractions.Shell;

/// <summary>
///     模块向 MainContent 贡献主视图的契约。
///     模块在 <see cref="Prism.Ioc.IContainerRegistry" /> 中以本接口注册实现；
///     SideBar 内交互请求打开主视图时（OpenMainViewEvent，负载为 <see cref="Id" />），
///     shell 找到对应贡献并经容器解析 <see cref="ViewType" /> 替换 MainContent 当前视图。
/// </summary>
public interface IMainViewContribution
{
    /// <summary>
    ///     主视图的稳定标识，跨模块全局唯一（shell 按 Id 索引全部贡献；建议以模块名做前缀，如 dashboard.overview）
    /// </summary>
    string Id { get; }

    /// <summary>
    ///     打开时 MainContent 显示的视图类型，经容器解析以支持依赖注入
    /// </summary>
    Type ViewType { get; }
}
